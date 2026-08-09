using Asp.Versioning;
using FinFlow.Orion.Api.Filters;
using FinFlow.Orion.Application;
using FinFlow.Orion.Infrastructure;
using FinFlow.Orion.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ── Controllers ───────────────────────────────────────────────────────
        services.AddControllers();

        // ── API Versioning ────────────────────────────────────────────────────
        services
            .AddApiVersioning(options =>
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

        // =====================================================================
        // NATIVE .NET 10 OPENAPI
        //
        // IMPORTANT:
        // Do NOT use:
        //
        // options.AddSecurityDefinition(...)
        // options.AddSecurityRequirement(...)
        //
        // Those are Swashbuckle APIs.
        //
        // Native .NET 10 OpenAPI uses transformers.
        // =====================================================================

        services.AddOpenApi(options =>
        {
            // -----------------------------------------------------------------
            // API DOCUMENT METADATA
            // -----------------------------------------------------------------

            options.AddDocumentTransformer(
                (document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "FinFlow.Orion API",
                        Version = "v1",
                        Description =
                            "Payment Orchestration & Reconciliation Engine",

                        Contact = new OpenApiContact
                        {
                            Name = "Mwangi Wa Mburu",
                            Url = new Uri(
                                "https://github.com/mburu1")
                        }
                    };

                    return Task.CompletedTask;
                });

            // -----------------------------------------------------------------
            // JWT BEARER SECURITY
            // -----------------------------------------------------------------

            options.AddDocumentTransformer<
                BearerSecuritySchemeTransformer>();

            // -----------------------------------------------------------------
            // IDEMPOTENCY-KEY
            // -----------------------------------------------------------------

            options.AddOperationTransformer<
                IdempotencyKeyOperationTransformer>();
        });

        // =====================================================================
        // JWT AUTHENTICATION
        // =====================================================================

        // Fail-fast sanity checks against the raw configuration — these catch
        // missing/placeholder secrets at startup, independent of how JwtBearer
        // itself later resolves its signing key (see below).
        var jwtKeyRaw = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKeyRaw))
        {
            throw new InvalidOperationException(
                "Configuration key 'Jwt:Key' is missing.");
        }

        if (environment.IsProduction() &&
            jwtKeyRaw.Contains("REPLACE_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Configuration key 'Jwt:Key' is still the placeholder value. " +
                "Set a real secret via user-secrets or the Jwt__Key environment " +
                "variable before running in Production.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
        {
            throw new InvalidOperationException(
                "Configuration key 'Jwt:Issuer' is missing.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        {
            throw new InvalidOperationException(
                "Configuration key 'Jwt:Audience' is missing.");
        }

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Bind JwtBearerOptions from IOptions<JwtConfiguration> (the same source
        // JwtTokenService uses to sign tokens) rather than capturing values from
        // raw IConfiguration at registration time. Keeping both consumers on one
        // options pipeline guarantees the signing and validation keys can never
        // diverge — e.g. when a test host overrides configuration after this
        // extension method runs.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtConfiguration>>((options, jwtConfig) =>
            {
                var config = jwtConfig.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Key)),

                    ValidateIssuer = true,
                    ValidIssuer = config.Issuer,

                    ValidateAudience = true,
                    ValidAudience = config.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        // =====================================================================
        // AUTHORIZATION
        // =====================================================================

        services.AddAuthorization();

        // =====================================================================
        // CORS
        // =====================================================================

        services.AddCors(options =>
        {
            options.AddPolicy(
                "FinFlowCors",
                policy =>
                {
                    var allowedOrigins =
                        configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>()
                        ?? ["http://localhost:3000"];

                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        // =====================================================================
        // APPLICATION
        // =====================================================================

        services.AddApplication();

        // =====================================================================
        // INFRASTRUCTURE
        // =====================================================================

        services.AddInfrastructure(configuration);

        // =====================================================================
        // MONGODB
        // =====================================================================

        var mongoConnectionString =
            configuration["MongoDB:ConnectionString"];

        if (string.IsNullOrWhiteSpace(mongoConnectionString))
        {
            throw new InvalidOperationException(
                "Configuration key " +
                "'MongoDB:ConnectionString' is missing.");
        }

        services.AddSingleton<IMongoClient>(
            _ => new MongoClient(mongoConnectionString));

        // =====================================================================
        // HEALTH CHECKS
        // =====================================================================

        var sqlConnectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string " +
                "'DefaultConnection' is missing.");
        }

        services
            .AddHealthChecks()
            .AddSqlServer(sqlConnectionString)
            .AddMongoDb();

        return services;
    }
}

// ============================================================================
// JWT BEARER SECURITY SCHEME TRANSFORMER
// ============================================================================

public sealed class BearerSecuritySchemeTransformer
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // ---------------------------------------------------------------------
        // Register Bearer security scheme
        // ---------------------------------------------------------------------

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Description =
                        "Enter your JWT bearer token."
                }
            };

        // ---------------------------------------------------------------------
        // Apply Bearer requirement to every operation
        //
        // This is the .NET 10 / OpenAPI.NET v2 approach.
        // ---------------------------------------------------------------------

        foreach (var pathItem in document.Paths.Values)
        {
            foreach (var operation in pathItem.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
            {
                operation.Security ??= [];

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document)
                        ] = []
                    });
            }
        }

        return Task.CompletedTask;
    }
}

// ============================================================================
// IDEMPOTENCY-KEY OPERATION TRANSFORMER
// ============================================================================

public sealed class IdempotencyKeyOperationTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var httpMethod =
            context.Description.HttpMethod?
                .ToUpperInvariant();

        // Only mutating operations require the header.
        if (httpMethod is not ("POST" or "PATCH" or "DELETE"))
        {
            return Task.CompletedTask;
        }

        // Prevent duplicate parameter registration.
        operation.Parameters ??= [];

        var alreadyExists =
            operation.Parameters.Any(parameter =>
                parameter.Name is not null &&
                parameter.Name.Equals(
                    "Idempotency-Key",
                    StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
        {
            return Task.CompletedTask;
        }

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = false,

                Description =
                    "Unique key used to ensure idempotent " +
                    "processing of the request. " +
                    "Minimum length: 16 characters.",

                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    MinLength = 16
                }
            });

        return Task.CompletedTask;
    }
}