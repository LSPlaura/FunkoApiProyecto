using System.Text;
using FunkoApi.Back.Config;
using FunkoApi.Back.Hub;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Middleware;
using FunkoApi.Back.Services.Email;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/funkos-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSerilog(Log.Logger);
builder.Host.UseSerilog(Log.Logger);

try {
    Log.Information("Iniciando la aplicación...");

    // ── CONFIGURACIÓN TIPADA ─────────────────────────────────────────────────
    builder.Services.Configure<ApiConfig>(
        builder.Configuration.GetSection("Api"));

    builder.Services.Configure<SmtpSettings>(
        builder.Configuration.GetSection("Email"));
    
    // ── IDENTITY + JWT ─────────────────────────────────────────────────────
    builder.Services.AddIdentityConfiguration(builder.Configuration, builder.Environment);

    // ── SERVICIOS ─────────────────────────────────────────────────────────────
    builder.Services.AddControllers(options =>
    {
        options.RespectBrowserAcceptHeader = true;
        options.ReturnHttpNotAcceptable = true;
    });

    builder.Services.AddSwaggerConfiguration();

    // Registrar SignalR para las conexiones del hub de Funkos.
    builder.Services.AddSignalR();
    // Leer los orígenes del entorno activo y conservar las credenciales de SignalR.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ClienteSignalR", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    builder.Services
        .AddCache(builder.Configuration)
        .AddDatabases(builder.Configuration)
        .AddSeeders(builder.Configuration)
        .AddRepositories(builder.Configuration)
        .AddStorage(builder.Configuration)
        .AddServices();

    builder.Services.AddGraphQlConfig(builder.Environment);

    // Permite probar SMTP en desarrollo sin cambiar el entorno de toda la API.
    var useSmtp = builder.Configuration.GetValue<bool>(
        "Email:UseSmtp", !builder.Environment.IsDevelopment());

    if (!useSmtp) {
        Log.Information("Email: modo simulado en memoria. No se enviarán correos reales.");
        builder.Services.AddScoped<IEmailService, MemoryEmailService>();
    } else {
        Log.Information("Email: envío real con MailKit.");
        builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    }
    builder.Services.AddSingleton<EmailQueueService>();
    builder.Services.AddHostedService<EmailBackgroundWorker>();

    // ── PIPELINE DE MIDDLEWARES ───────────────────────────────────────────────
    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseGlobalExceptionHandler();
    // Aplicar la política antes de comprobar autenticación y permisos.
    app.UseCors("ClienteSignalR");
    
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapGraphQlEndpoints();

    if (app.Environment.IsDevelopment()) {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    // Publicar el hub; su atributo Authorize limita la conexión al rol ADMIN.
    app.MapHub<FunkoHub>("/hubs/funkos");
    
    app.InitializeStorage(app.Configuration);
    await app.InitializeDatabaseAsync();

    // ── CONFIGURACIÓN TIPADA: se lee y se muestra al arrancar ────────────────
    var apiConfig = app.Services.GetRequiredService<IOptions<ApiConfig>>().Value;

    Log.Information("API {Nombre}: {Mensaje}", apiConfig.Nombre, apiConfig.Mensaje);

    var port = builder.Configuration["ASPNETCORE_URLS"]?.Split(':').LastOrDefault() ?? "5000";
    Console.WriteLine("========================================");
    Console.WriteLine($"  {apiConfig.Nombre}");
    Console.WriteLine($"  {apiConfig.Mensaje}");
    Console.WriteLine($"  http://localhost:{port}");
    Console.WriteLine($"  Swagger: http://localhost:{port}/swagger");
    Console.WriteLine($"  Entorno: {app.Environment.EnvironmentName}");
    Console.WriteLine("========================================");

    await app.RunAsync();
} catch (Exception ex) {
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
} finally {
    await Log.CloseAndFlushAsync();
}
