using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Theoremone.SmartAc.Application.Constants;
using Theoremone.SmartAc.Application.Identity;
using Theoremone.SmartAc.Application.Interfaces;
using Theoremone.SmartAc.Application.Interfaces.Identity;
using Theoremone.SmartAc.Application.Models;
using Theoremone.SmartAc.Application.Services;
using Theoremone.SmartAc.Application.Services.Identity;
using Theoremone.SmartAc.Application.Settings;
using Theoremone.SmartAc.Infrastructure.Data;
using Theoremone.SmartAc.Infrastructure.DbContexts;
using Theoremone.SmartAc.Validations;

namespace Theoremone.SmartAc;

internal static class ConfigurationExtensions
{
    public static void AddSmartAcServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("smartac") ?? "Data Source=SmartAc.db";

        services.AddSqlite<SmartAcContext>(connectionString);
        services.AddTransient<ISmartAcJwtService, SmartAcJwtService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IValidator<DeviceReadingRecord>, DeviceReadingRecordValidation>();

        services.AddTransient<IAuthorizationHandler, ValidTokenAuthorizationHandler>();
    }

    public static void AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartAC API",
                Description = "SmartAC Device Reporting API",
                Version = "v1"
            });

            c.AddSecurityDefinition("BearerAuth", new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "BearerAuth"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });
    }

    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var signingKey = configuration["Jwt:Key"];

        services.AddAuthorization();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidIssuer = issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("DeviceAdmin", policy =>
                policy.RequireRole(SmartJwtServiceConstants.JwtScopeDeviceAdminService)
            );

            options.AddPolicy("DeviceIngestion", policy =>
            {
                policy.RequireRole(SmartJwtServiceConstants.JwtScopeDeviceIngestionService);
                policy.AddRequirements(new ValidTokenRequirement());
            });
        });

        services.AddHttpContextAccessor();
    }

    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAC API V1"); });
    }

    public static void MapSmartAcControllers(this WebApplication app)
    {
        app.MapControllers();
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
    }

    public static void EnsureDatabaseSetup(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<SmartAcContext>();
        db.Database.Migrate();
        SmartAcDataSeeder.Seed(db);
    }
    public static void AddAppSettingsConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SensorLimitsOptions>(configuration.GetSection(SensorLimitsOptions.SensorLimits));
        services.Configure<GeneralSettingsOptions>(configuration.GetSection(GeneralSettingsOptions.GeneralSettings));
    }
}