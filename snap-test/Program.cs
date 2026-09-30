using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using snap_test.GraphQL;
using snap_test.Middleware;
using snap_test.Swagger;
using System.Text;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

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

// APIBee: permissive CORS for a public, no-signup API. Exposes pagination + simulation headers.
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
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
// Docs are public in every environment: APIBee is a public test API and the docs are part of the product.
app.UseApiBeeSwagger();
app.MapGraphQL("/graphql");

app.UseHttpsRedirection();

app.UseCors("ApiBee");

// APIBee: standard + informational headers on every response.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Powered-By"] = "APIBee";
    context.Response.Headers["X-RateLimit-Limit"] = "1000";
    context.Response.Headers["X-RateLimit-Remaining"] = "999";
    context.Response.Headers["X-RateLimit-Reset"] = "1721300000";
    await next();
});

// APIBee: ?delay= / ?error= simulation (must run before controllers).
app.UseMiddleware<SimulationMiddleware>();

// WebSocket test endpoints (/ws/echo, /ws/ticker).
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
