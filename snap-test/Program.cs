using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using snap_test.GraphQL;
using snap_test.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();

// APIBee: permissive CORS for a public, no-signup API. Exposes pagination + simulation headers.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiBee", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Total-Count", "X-Page", "X-Per-Page", "X-Total-Pages", "X-Simulated"));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
