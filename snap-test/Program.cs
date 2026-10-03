using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using snap_test.GraphQL;
using snap_test.Helpers;
using snap_test.Middleware;
using snap_test.Swagger;
using System.Text;
using System.Text.Encodings.Web;

// Read appsettings.json from the app's own folder rather than the shell's current directory, so the released
// binaries work when launched from anywhere (e.g. ./apibee-1.0.0-linux-x64/snap-test).
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Cloud hosts such as Render, Railway, Heroku and Cloud Run tell the app which port to listen on via PORT.
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var hostPort))
    builder.WebHost.UseUrls($"http://0.0.0.0:{hostPort}");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddXmlSerializerFormatters()
    // Emit emoji / non-Latin text as-is instead of \uXXXX escapes (still valid JSON).
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        // Echo-style endpoints wrap deeply nested client input one or two levels deeper; the default of 32 is too low.
        o.JsonSerializerOptions.MaxDepth = 256;
    });

// TestingAPIs: permissive CORS for a public, no-signup API. Exposes pagination + simulation headers.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiBee", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Total-Count", "X-Page", "X-Per-Page", "X-Total-Pages", "X-Simulated",
                                  "ETag", "Last-Modified", "Location", "Link", "Retry-After", "Content-Disposition",
                                  "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset",
                                  "Idempotent-Replayed", "X-Request-Id", "Deprecation", "Sunset", "X-API-Version"));
});
// Swagger / OpenAPI: document at /openapi/v1.json, UI at /swagger (see Swagger/SwaggerSetup.cs).
builder.Services.AddApiBeeSwagger();
builder.Services.AddHttpClient();
// Guarded HttpClient for /api/Proxy/call: blocks private/internal destinations (Proxy:AllowPrivateNetworks to opt out locally).
builder.Services.AddGuardedProxyClient(builder.Configuration);
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

// Fail fast at startup: a missing key would otherwise surface as a 500 on every request.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing configuration value 'Jwt:Key'. Set it in appsettings.json or the Jwt__Key environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
// Docs are public in every environment: TestingAPIs is a public test API and the docs are part of the product.
app.UseApiBeeSwagger();
app.MapGraphQL("/graphql");

app.UseHttpsRedirection();

app.UseCors("ApiBee");

// TestingAPIs: standard + informational headers on every response.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Powered-By"] = "TestingAPIs";
    context.Response.Headers["X-RateLimit-Limit"] = "1000";
    context.Response.Headers["X-RateLimit-Remaining"] = "999";
    context.Response.Headers["X-RateLimit-Reset"] = "1721300000";
    await next();
});

// TestingAPIs: ?delay= / ?error= simulation (must run before controllers).
app.UseMiddleware<SimulationMiddleware>();

// WebSocket test endpoints (/ws/echo, /ws/ticker).
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
