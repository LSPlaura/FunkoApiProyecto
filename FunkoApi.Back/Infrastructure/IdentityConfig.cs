using System.Text;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configuracion de Identity + JWT + Authorization.
/// </summary>
public static class IdentityConfig
{
    /// <summary>
    /// Registra Identity, JWT Bearer y políticas de autorización.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <param name="environment">Entorno de la aplicación.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddIdentityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        Log.Information("Configurando Identity y JWT...");

        // ── JWT Settings ─────────────────────────────────────────────────────
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        // ── Identity ─────────────────────────────────────────────────────────
        // configuración manual de identity para AppUser especificando:
        //contraseñas válidas, intentos hasta bloquea la cuenta, tiempo de bloqueo
        //especifica que el email debe ser único
        //y se añade además: los roles, el servicio propio para registrarse (Sing in)
        //conecta el sistema de Identity con tu base de datos utilizando Entity Framework Core
        //registra los proveedores estándar encargados de generar tokens de un solo uso (OTP)
        services.AddIdentityCore<AppUser>(options =>
        {
            // Password
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            // Lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<long>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── JWT Authentication ───────────────────────────────────────────────
        //Se fuerza todos los esquemas a Bearer para que Identity sepa que es una API de manera que,
        //en vez de redigir mande el código de response adecuado (401 o 403) mediante un json
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // ── Authorization Policies ───────────────────────────────────────────
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("ADMIN"))
            .AddPolicy("RequireUser", policy =>
                policy.RequireRole("USER", "ADMIN"));

        Log.Information("Identity y JWT configurados correctamente.");
        return services;
    }
}
