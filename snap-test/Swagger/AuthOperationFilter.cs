using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace snap_test.Swagger
{
    /// <summary>
    /// Marks each operation with the auth scheme it expects, so Swagger UI shows a lock icon and sends the
    /// credentials entered under "Authorize". [Authorize] and [BasicAuth] are detected from attributes;
    /// the /api/auth/* test endpoints check credentials by hand, so they are mapped by route.
    /// </summary>
    public class AuthOperationFilter : IOperationFilter
    {
        // Route prefix (lower-case, no leading slash) -> schemes that satisfy it. First match wins.
        private static readonly (string Prefix, string[] Schemes)[] RouteSchemes =
        {
            ("api/auth/basic", new[] { SecuritySchemes.Basic }),
            ("api/auth/bearer", new[] { SecuritySchemes.Bearer }),
            ("api/auth/roles/", new[] { SecuritySchemes.Bearer }),
            ("api/auth/api-key/header", new[] { SecuritySchemes.ApiKeyHeader }),
            ("api/auth/api-key/query", new[] { SecuritySchemes.ApiKeyQuery }),
            ("api/auth/oauth/protected", new[] { SecuritySchemes.OAuth2, SecuritySchemes.Bearer }),
            ("api/auth/oauth/userinfo", new[] { SecuritySchemes.OAuth2, SecuritySchemes.Bearer }),
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var schemes = SchemesFor(context);
            if (schemes.Length == 0) return;

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            foreach (var scheme in schemes)
            {
                // Each requirement is an alternative: any one of the listed schemes is enough.
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(scheme, context.Document)] = new List<string>()
                });
            }
        }

        private static string[] SchemesFor(OperationFilterContext context)
        {
            var method = context.MethodInfo;
            var controller = method.DeclaringType!;

            var allowAnonymous = method.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();
            if (!allowAnonymous &&
                (method.GetCustomAttributes<AuthorizeAttribute>(true).Any() || controller.GetCustomAttributes<AuthorizeAttribute>(true).Any()))
                return new[] { SecuritySchemes.Bearer };

            if (method.GetCustomAttributes<BasicAuthAttribute>(true).Any() || controller.GetCustomAttributes<BasicAuthAttribute>(true).Any())
                return new[] { SecuritySchemes.Basic };

            var path = (context.ApiDescription.RelativePath ?? string.Empty).ToLowerInvariant();
            foreach (var (prefix, schemes) in RouteSchemes)
            {
                if (path.StartsWith(prefix, StringComparison.Ordinal))
                    return schemes;
            }

            return Array.Empty<string>();
        }
    }
}
