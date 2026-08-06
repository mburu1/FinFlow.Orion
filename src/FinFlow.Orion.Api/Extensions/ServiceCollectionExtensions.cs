using Asp.Versioning;
using FinFlow.Orion.Api.Filters;
using FinFlow.Orion.Application;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Ledger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;               // OpenAPI.NET v2: Models/Models.Security namespaces are gone,
                                       // everything (OpenApiInfo, OpenApiSecurityScheme, OpenApiSchema,
                                       // OpenApiOperation, JsonSchemaType, ParameterLocation, ...) lives here.
using MongoDB.Driver;
using Swashbuckle.AspNetCore.SwaggerGen;
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

        // ── Swagger / OpenAPI ─────────────────────────────────────────────────
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FinFlow.Orion API",
                Version = "v1",
                Description = "Payment Orchestration & Reconciliation Engine",
                Contact = new OpenApiContact
                {
                    Name = "Mwangi Wa Mburu",
                    Url = new Uri("https://github.com/mburu1")
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token."
            });

            // FIX: OpenApiSecurityScheme.Reference was removed in OpenAPI.NET v2.
            // AddSecurityRequirement now takes a Func<OpenApiDocument, OpenApiSecurityRequirement>
            // so the reference can be built against the document currently being generated.
            // The scheme id string ("Bearer") passed to OpenApiSecuritySchemeReference must match
            // the AddSecurityDefinition id exactly, including case.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            options.OperationFilter<IdempotencyKeyOperationFilter>();
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
        // AspNetCore.HealthChecks.MongoDb v9 no longer accepts a connection string
        // and no longer caches clients internally — it resolves IMongoClient from DI.
        // MongoClient is documented as safe/intended to be a singleton (it owns the
        // connection pool), so register it once here rather than per health-check call.
        services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(
                configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Configuration key 'MongoDB:ConnectionString' is missing.")));

        // ── Health checks ─────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!)
            .AddMongoDb(); // resolves the IMongoClient singleton registered above

        return services;
    }
}

// ── Swagger Operation Filter — Idempotency-Key header ────────────────────────

public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpper();
        if (httpMethod is not ("POST" or "PATCH" or "DELETE"))
            return;

        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Unique key to ensure idempotent requests. Min 16 characters.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = 16
            }
        });
    }
}