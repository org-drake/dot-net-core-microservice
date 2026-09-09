using dot_net_core_microservice.Application;
using dot_net_core_microservice.Authentication;
using dot_net_core_microservice.Infrastructure;
using dot_net_core_microservice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// AzureAd:TenantId/Audience are not secret (they're public identifiers, and
// token signing keys are fetched from Entra's public JWKS endpoint). Bearer
// auth is opt-in: leave both unset (e.g. local dev) and the API only accepts
// Basic Auth. Set both to enable it - supply them via the AzureAd:TenantId /
// AzureAd:Audience user-secrets keys locally, or the AzureAd__TenantId /
// AzureAd__Audience environment variables. Register the API app with
// scripts/register-entra-api.sh.
var azureAdTenantId = builder.Configuration["AzureAd:TenantId"];
var azureAdAudience = builder.Configuration["AzureAd:Audience"];
if (string.IsNullOrEmpty(azureAdTenantId) != string.IsNullOrEmpty(azureAdAudience))
{
    throw new InvalidOperationException(
        "AzureAd:TenantId and AzureAd:Audience must both be set to enable Bearer auth, or both left " +
        "unset to disable it - only one is currently configured.");
}

var bearerEnabled = !string.IsNullOrEmpty(azureAdTenantId);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var basicScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" }
    };
    options.AddSecurityDefinition("basic", basicScheme);
    // Two separate requirements, not one with both schemes, so Swagger UI treats
    // them as alternatives (either satisfies) rather than both being required.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { basicScheme, Array.Empty<string>() } });

    if (bearerEnabled)
    {
        var bearerScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" }
        };
        options.AddSecurityDefinition("bearer", bearerScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearerScheme, Array.Empty<string>() } });
    }
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
builder.Services.AddProblemDetails();

// BasicAuth:User/Password must never be committed - supply them via the
// BasicAuth:Username / BasicAuth:Password user-secrets keys locally, or the
// BasicAuth__Username / BasicAuth__Password environment variables (e.g. from
// a k8s Secret), same as the Sql:* credentials.
var basicAuthUsername = builder.Configuration["BasicAuth:Username"];
var basicAuthPassword = builder.Configuration["BasicAuth:Password"];
if (string.IsNullOrEmpty(basicAuthUsername) || string.IsNullOrEmpty(basicAuthPassword))
{
    throw new InvalidOperationException(
        "Basic auth credentials are not configured. Set BasicAuth:Username and BasicAuth:Password via " +
        "dotnet user-secrets (local dev) or the BasicAuth__Username / BasicAuth__Password environment " +
        "variables (e.g. sourced from a Kubernetes Secret).");
}

var authenticationBuilder = builder.Services
    .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationDefaults.AuthenticationScheme, null);

var authenticationSchemes = new List<string> { BasicAuthenticationDefaults.AuthenticationScheme };

if (bearerEnabled)
{
    var azureAdInstance = builder.Configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
    authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = $"{azureAdInstance}{azureAdTenantId}/v2.0";
        options.Audience = azureAdAudience;
    });
    authenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
}

// [Authorize] with no explicit scheme falls back to this policy, so controllers
// don't need to know which schemes happen to be enabled.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(authenticationSchemes.ToArray())
        .RequireAuthenticatedUser()
        .Build();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllers();

app.Run();
