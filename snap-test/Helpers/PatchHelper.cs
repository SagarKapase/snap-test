using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace snap_test.Helpers
{
    /// <summary>
    /// JSON Merge Patch (RFC 7396 style): only the properties present in the body are changed, and nested
    /// objects are merged recursively rather than replaced. Property names match case-insensitively;
    /// "id" is never changed. Unknown fields are reported, not applied.
    /// </summary>
    public static class PatchHelper
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static (List<string> changed, List<string> ignored) Apply<T>(T target, JsonElement patch)
        {
            if (patch.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Patch body must be a JSON object.");

            var changed = new List<string>();
            var ignored = new List<string>();
            ApplyObject(target!, typeof(T), patch, "", changed, ignored);
            return (changed, ignored);
        }

        private static void ApplyObject(object target, Type type, JsonElement patch, string prefix,
                                        List<string> changed, List<string> ignored)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();

            foreach (var field in patch.EnumerateObject())
            {
                var prop = props.FirstOrDefault(p => string.Equals(p.Name, field.Name, StringComparison.OrdinalIgnoreCase));
                if (prop == null || (prefix == "" && prop.Name == nameof(IEntity.Id)))
                {
                    ignored.Add(prefix + field.Name);
                    continue;
                }

                var path = prefix + JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                var current = prop.GetValue(target);

                // Nested object onto an existing nested object -> merge instead of replace.
                if (field.Value.ValueKind == JsonValueKind.Object && current != null && IsMergeable(prop.PropertyType))
                {
                    ApplyObject(current, prop.PropertyType, field.Value, path + ".", changed, ignored);
                    continue;
                }

                if (field.Value.ValueKind == JsonValueKind.Null && !AcceptsNull(prop))
                    throw new ArgumentException($"Field '{path}' cannot be null.");

                try
                {
                    prop.SetValue(target, field.Value.Deserialize(prop.PropertyType, Options));
                }
                catch (JsonException)
                {
                    throw new ArgumentException($"Field '{path}' has an invalid value for type {FriendlyName(prop.PropertyType)}.");
                }

                changed.Add(path);
            }
        }

        private static bool IsMergeable(Type t) =>
            t.IsClass && t != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(t);

        private static bool AcceptsNull(PropertyInfo prop)
        {
            if (prop.PropertyType.IsValueType) return Nullable.GetUnderlyingType(prop.PropertyType) != null;
            return new NullabilityInfoContext().Create(prop).WriteState != NullabilityState.NotNull;
        }

        private static string FriendlyName(Type t)
        {
            var u = Nullable.GetUnderlyingType(t) ?? t;
            return u.IsArray || (u != typeof(string) && typeof(IEnumerable).IsAssignableFrom(u)) ? "array" : u.Name.ToLowerInvariant();
        }
    }
}
