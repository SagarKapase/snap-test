using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace snap_test.Swagger
{
    /// <summary>
    /// Swagger / OpenAPI wiring. The JSON document is served at /openapi/v1.json (same URL the old
    /// Microsoft.AspNetCore.OpenApi setup used) and the interactive UI at /swagger.
    /// </summary>
    public static class SwaggerSetup
    {
        public const string DocumentName = "v1";

        private const string Description = """
            **APIBee** — hardcoded dummy APIs for testing HTTP clients, API tools and automation suites.
            All data is in-memory and resets when the server restarts.

            ### Works on every endpoint
            | Query param | Effect |
            |---|---|
            | `?delay=N` | Wait N seconds (max 10) before responding |
            | `?error=CODE` | Return a simulated error (400, 401, 403, 404, 408, 429, 500, 502, 503) with `X-Simulated: true` |

            List endpoints also accept `limit`, `page`, `offset`, `sort`, `order`, `q` and any field name as a filter,
            and return `X-Total-Count`, `X-Page`, `X-Per-Page` and `X-Total-Pages` headers.

            ### Test credentials
            | Scheme | Value |
            |---|---|
            | Basic (`/api/auth/*`) | `apibee` / `password123` |
            | Basic (`/api/AuthTest`) | `Admin` / `Admin@1234` |
            | Bearer (static) | `apibee-token-123` · role tokens `admin-token`, `user-token`, `readonly-token` |
            | API key | `apibee-key-123` (header `X-API-Key` or query `api_key`) |
            | JWT | `POST /api/auth/jwt/login` with `admin`/`admin123` or `user`/`user123` · legacy `POST /api/User/Login` with `Michael`/`Thompson` |
            | OAuth2 | client `apibee-client` / `apibee-secret`, public PKCE client `apibee-public`, users `apibee`/`password123` |
            | HMAC secret | `apibee-hmac-secret` |

            ### Not shown here
            - **GraphQL**: `POST /graphql` (open `/graphql` in a browser for the GraphQL IDE)
            - **WebSockets**: `ws://{host}/ws/echo`, `ws://{host}/ws/ticker`
            - **Route catalog**: `GET /api` lists every live route
            """;

        public static IServiceCollection AddApiBeeSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(DocumentName, new OpenApiInfo
                {
                    Title = "APIBee — snap-test API",
                    Version = "v1",
                    Description = Description
                });

                // Action + controller summaries from /// comments (GenerateDocumentationFile in the csproj).
                var xml = System.IO.Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                if (File.Exists(xml))
                    c.IncludeXmlComments(xml, includeControllerXmlComments: true);

                // Group the sidebar by category, then by path.
                c.OrderActionsBy(a => $"{TagCatalog.SortKey(a.ActionDescriptor.RouteValues["controller"])}_{a.RelativePath}_{a.HttpMethod}");

                // The /ws/* actions are route-only (any verb) so HTTP/2 WebSockets, which arrive as CONNECT, still match.
                // OpenAPI needs one verb per operation, so document them as GET: the HTTP/1.1 upgrade request.
                c.DocInclusionPredicate((_, api) =>
                {
                    api.HttpMethod ??= "GET";
                    return true;
                });

                // Same path + verb declared twice (e.g. catch-all echo routes) -> document the first.
                c.ResolveConflictingActions(descriptions => descriptions.First());

                // Stable operationIds (used by code generators and Postman import), e.g. "Products_GetById".
                // Actions bound to several verbs or routes are de-duplicated in TagCatalog.
                c.CustomOperationIds(api => $"{api.ActionDescriptor.RouteValues["controller"]}_{api.ActionDescriptor.RouteValues["action"]}");

                // Nested types such as Rating / CartItem get unique, readable schema ids.
                c.CustomSchemaIds(SchemaId);

                AddSecuritySchemes(c);
                c.OperationFilter<AuthOperationFilter>();
                c.OperationFilter<RequestHintsOperationFilter>();
                c.DocumentFilter<TagCatalog>();
            });

            return services;
        }

        public static WebApplication UseApiBeeSwagger(this WebApplication app)
        {
            app.UseSwagger(o => o.RouteTemplate = "openapi/{documentName}.json");
            app.UseSwaggerUI(ui =>
            {
                ui.SwaggerEndpoint($"/openapi/{DocumentName}.json", "APIBee v1");
                ui.RoutePrefix = "swagger";
                ui.DocumentTitle = "APIBee API docs";
                ui.DocExpansion(DocExpansion.None);   // 400+ operations: start collapsed
                ui.EnableFilter();                    // tag search box
                ui.EnableDeepLinking();
                ui.DisplayRequestDuration();
                ui.EnablePersistAuthorization();
                ui.EnableTryItOutByDefault();
                ui.DefaultModelsExpandDepth(0);

                // OAuth2 "Authorize" button: public client + PKCE, no secret needed in the browser.
                ui.OAuthClientId("apibee-public");
                ui.OAuthAppName("APIBee Swagger UI");
                ui.OAuthUsePkce();
            });

            // Landing on the bare host opens the docs.
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

            return app;
        }

        private static void AddSecuritySchemes(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions c)
        {
            c.AddSecurityDefinition(SecuritySchemes.Basic, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "`apibee` / `password123` for /api/auth/*, `Admin` / `Admin@1234` for /api/AuthTest."
            });
            c.AddSecurityDefinition(SecuritySchemes.Bearer, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT or opaque token",
                Description = "A JWT from /api/auth/jwt/login (or /api/User/Login), an OAuth access token, " +
                              "or a static token: `apibee-token-123`, `admin-token`, `user-token`, `readonly-token`."
            });
            c.AddSecurityDefinition(SecuritySchemes.ApiKeyHeader, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "`apibee-key-123`"
            });
            c.AddSecurityDefinition(SecuritySchemes.ApiKeyQuery, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Query,
                Name = "api_key",
                Description = "`apibee-key-123`"
            });
            c.AddSecurityDefinition(SecuritySchemes.OAuth2, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Authorization code + PKCE with public client `apibee-public` (user `apibee` / `password123`), " +
                              "or client credentials with `apibee-client` / `apibee-secret`.",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("/api/auth/oauth/authorize", UriKind.Relative),
                        TokenUrl = new Uri("/api/auth/oauth/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string>
                        {
                            ["read"] = "Read access",
                            ["write"] = "Write access",
                            ["profile"] = "User profile"
                        }
                    },
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("/api/auth/oauth/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string> { ["read"] = "Read access", ["write"] = "Write access" }
                    },
                    Password = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("/api/auth/oauth/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string> { ["read"] = "Read access", ["profile"] = "User profile" }
                    }
                }
            });
        }

        // "Rating" stays "Rating"; nested "Outer+Inner" becomes "OuterInner"; generics become "ListOfBook".
        private static string SchemaId(Type type)
        {
            if (type.IsGenericType)
            {
                var name = type.Name[..type.Name.IndexOf('`')];
                return name + "Of" + string.Join("And", type.GetGenericArguments().Select(SchemaId));
            }

            return type.DeclaringType != null ? SchemaId(type.DeclaringType) + type.Name : type.Name;
        }
    }

    public static class SecuritySchemes
    {
        public const string Basic = "basic";
        public const string Bearer = "bearer";
        public const string ApiKeyHeader = "apiKeyHeader";
        public const string ApiKeyQuery = "apiKeyQuery";
        public const string OAuth2 = "oauth2";
    }
}
