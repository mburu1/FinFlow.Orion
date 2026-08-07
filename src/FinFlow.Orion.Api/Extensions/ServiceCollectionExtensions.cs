using Asp.Versioning;
using FinFlow.Orion.Api.Filters;
using FinFlow.Orion.Application;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Ledger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using System.Text;

namespace FinFlow.Orion.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Controllers ───────────────────────────────────────────────────────
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        // ── API Versioning ────────────────────────────────────────────────────
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // ── Native .NET 10 OpenAPI (no Swashbuckle) ───────────────────────────
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "FinFlow.Orion API",
                    Version = "v1",
                    Description = "Payment Orchestration & Reconciliation Engine",
                    Contact = new()
                    {
                        Name = "Mwangi Wa Mburu",
                        Url = new Uri("https://github.com/mburu1")
                    }
                };
                return Task.CompletedTask;
            });

            // ── JWT Bearer security scheme ─────────────────────────────────────
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

            // ── Idempotency-Key header on mutating operations ──────────────────
            options.AddOperationTransformer<IdempotencyKeyOperationTransformer>();
        });

        // ── Authentication ────────────────────────────────────────────────────
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            configuration["Jwt:Key"] ?? string.Empty))
                };
            });

        services.AddAuthorization();

        // ── CORS ──────────────────────────────────────────────────────────────
        services.AddCors(options =>
        {
            options.AddPolicy("FinFlowCors", policy =>
            {
                policy
                    .WithOrigins(
                        configuration.GetSection("Cors:AllowedOrigins")
                            .Get<string[]>() ?? ["http://localhost:3000"])
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // ── Application + Infrastructure + Ledger ─────────────────────────────
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddLedger();

        // ── MongoDB client ────────────────────────────────────────────────────
        services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(
                configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Configuration key 'MongoDB:ConnectionString' is missing.")));

        // ── Health checks ─────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!)
            .AddMongoDb();

        return services;
    }
}

// ── Bearer security scheme transformer ───────────────────────────────────────

public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token."
        };

        // Apply globally to all operations.
        // OpenAPI.NET v2 dropped OpenApiSecurityScheme.Reference / OpenApiReference in
        // favor of dedicated *Reference types, and OpenApiDocument.SecurityRequirements
        // was renamed to OpenApiDocument.Security.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}

// ── Idempotency-Key operation transformer ─────────────────────────────────────

public sealed class IdempotencyKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var httpMethod = context.Description.HttpMethod?.ToUpper();
        if (httpMethod is not ("POST" or "PATCH" or "DELETE"))
            return Task.CompletedTask;

        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Unique key to ensure idempotent requests. Min 16 characters.",
            Schema = new OpenApiSchema
            {
                // OpenApiSchema.Type is now the JsonSchemaType flags enum, not a string.
                Type = JsonSchemaType.String,
                MinLength = 16
            }
        });

        return Task.CompletedTask;
    }
}