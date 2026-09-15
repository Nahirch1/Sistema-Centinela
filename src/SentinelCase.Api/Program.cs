using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using SentinelCase.Api.Common.Authorization;
using SentinelCase.Api.Common.ExceptionHandling;
using SentinelCase.Api.Common.Identity;
using SentinelCase.Api.Endpoints;
using SentinelCase.Api.Endpoints.Auth;
using SentinelCase.AI;
using SentinelCase.Application;
using SentinelCase.Application.Common.Interfaces;
using SentinelCase.Infrastructure;
using SentinelCase.Infrastructure.Identity;
using SentinelCase.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override(
            "Microsoft.AspNetCore",
            LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                "[TraceId:{TraceId}] [User:{User}] " +
                "{Message:lj}{NewLine}{Exception}"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAI(builder.Configuration);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<
        SentinelCase.Infrastructure.Persistence.ApplicationDbContext>(
        "database");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                var partitionKey =
                    httpContext.Connection.RemoteIpAddress?
                        .ToString()
                    ?? "unknown";

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
            });
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("SentinelCase.Api"))
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter())
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(SentinelCaseMetrics.MeterName)
            .AddOtlpExporter());

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy =>
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var isTesting =
    builder.Environment.IsEnvironment("Testing");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? (isTesting
        ? "SistemaCentinela.Tests"
        : throw new InvalidOperationException(
            "Jwt:Issuer is missing."));

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? (isTesting
        ? "SistemaCentinela.Tests"
        : throw new InvalidOperationException(
            "Jwt:Audience is missing."));

var jwtSigningKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? (isTesting
        ? "SistemaCentinela_Test_Signing_Key_2026_AtLeast_32_Bytes"
        : throw new InvalidOperationException(
            "Jwt:SigningKey is missing."));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        AppPolicies.CanCreateIncident,
        policy => policy.RequireRole(
            AppRoles.Analyst,
            AppRoles.SocManager,
            AppRoles.Administrator))
    .AddPolicy(
        AppPolicies.CanManageIncidentStatus,
        policy => policy.RequireRole(
            AppRoles.SocManager,
            AppRoles.Administrator))
    .AddPolicy(
        AppPolicies.CanAssignIncident,
        policy => policy.RequireRole(
            AppRoles.SocManager,
            AppRoles.Administrator));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (
        diagnosticContext,
        httpContext) =>
    {
        diagnosticContext.Set(
            "TraceId",
            httpContext.TraceIdentifier);

        diagnosticContext.Set(
            "User",
            httpContext.User.Identity?.Name
                ?? "anonymous");
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await IdentitySeeder.SeedAsync(
        app.Services);
}

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    const string correlationHeader = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers.TryGetValue(
            correlationHeader,
            out var providedCorrelationId)
        && !string.IsNullOrWhiteSpace(providedCorrelationId)
            ? providedCorrelationId.ToString()
            : Guid.NewGuid().ToString();

    context.TraceIdentifier = correlationId;

    context.Response.Headers[correlationHeader] =
        correlationId;

    await next();
});

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions =
        "nosniff";

    context.Response.Headers.XFrameOptions =
        "DENY";

    context.Response.Headers.Append(
        "Referrer-Policy",
        "no-referrer");

    context.Response.Headers.Append(
        "Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");

    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'none'; frame-ancestors 'none'");

    await next();
});

app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseRateLimiter();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready");

app.MapControllers();
app.MapIncidentEndpoints();
app.MapAuthEndpoints();

app.Run();

public partial class Program;
