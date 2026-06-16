using Karavul.Data.Database;
using Karavul.Host.Configuration;
using Karavul.Host.Extensions;
using Karavul.Host.Workers;
using Karavul.Host.Localization;
using Karavul.Services;
using Serilog;
using Microsoft.AspNetCore.DataProtection;

//  Dizin oluşturma (ProgramData) 
var programDataPath = @"C:\ProgramData\Karavul";
var logsPath = Path.Combine(programDataPath, "logs");
Directory.CreateDirectory(programDataPath);
Directory.CreateDirectory(logsPath);

//  Serilog bootstrap logger 
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsPath, "karavul-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Karavul başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    //  Windows Service desteği 
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "KaravulService";
    });

    builder.Host.UseSerilog((ctx, cfg) =>
    {
        var settings = ctx.Configuration.GetSection("Karavul").Get<KaravulSettings>() ?? new();
        cfg.MinimumLevel.Information()
           .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
           .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
           .WriteTo.Console()
           .WriteTo.File(
               Path.Combine(settings.LogPath, "karavul-.log"),
               rollingInterval: RollingInterval.Day,
               retainedFileCountLimit: 30);
    });

    //  Kestrel – yalnızca localhost 
    var settings = builder.Configuration.GetSection("Karavul").Get<KaravulSettings>() ?? new();
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        // Sadece loopback – dış erişim yok
        kestrel.ListenLocalhost(settings.WebPort);
    });

    //  Servisler 
    builder.Services.AddKaravulServices(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
    builder.Services.AddSingleton<Karavul.Host.Services.RealtimeGraphService>();

    // Razor Pages + Session (auth için)
    builder.Services.AddRazorPages();
    builder.Services.AddKaravulHealthChecks();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = builder.Environment.IsDevelopment() ? ".Karavul.Session.Dev" : ".Karavul.Session";
    });

    // Background Workers
    builder.Services.AddHostedService<MonitorWorker>();
    builder.Services.AddHostedService<CleanupWorker>();

    var app = builder.Build();

    //  Startup: Schema init + Seed 
    using (var scope = app.Services.CreateScope())
    {
        var schemaInit = scope.ServiceProvider.GetRequiredService<SchemaInitializer>();
        await schemaInit.InitializeAsync();

        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.SeedDefaultUserAsync();

        var monitorRepo = scope.ServiceProvider.GetRequiredService<Karavul.Core.Interfaces.IMonitorRepository>();
        var existingMonitors = await monitorRepo.GetAllAsync();
        if (!existingMonitors.Any())
        {
            await monitorRepo.CreateAsync(new Karavul.Core.Entities.MonitorTarget
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Karavul",
                Url = $"http://127.0.0.1:{settings.WebPort}/Health",
                MonitorType = Karavul.Core.Enums.MonitorType.Http,
                HttpMethod = "GET",
                ExpectedStatusCode = 200,
                CheckIntervalSeconds = 120,
                MaxResponseTimeMs = 1000,
                Description = "Karavul sisteminin kendi iç sağlık durumu izleyicisi.",
                IsActive = true,
                CheckSsl = false,
                IsHealthJson = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            Log.Information("Varsayılan Karavul monitörü oluşturuldu.");
        }

        Log.Information("Veritabanı hazır. Web arayüzü: http://127.0.0.1:{Port}", settings.WebPort);
    }

    //  Middleware pipeline 
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }

    app.UseStaticFiles();
    app.UseWebSockets();
    app.UseRouting();
    app.UseSession();

    // Auth middleware – login olmayan kullanıcıyı yönlendir
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? "";
        var publicPaths = new[] { "/Login", "/ChangePassword", "/SetLanguage", "/css", "/js", "/lib", "/favicon", "/Health" };

        bool isPublic = publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isPublic && context.Session.GetString("UserId") == null)
        {
            if (context.Request.Cookies.TryGetValue("Karavul.RememberMe", out var rememberCookie) && !string.IsNullOrEmpty(rememberCookie))
            {
                try
                {
                    var dpProvider = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
                    var protector = dpProvider.CreateProtector("Karavul.RememberMe");
                    var userId = protector.Unprotect(rememberCookie);
                    
                    var authService = context.RequestServices.GetRequiredService<Karavul.Services.AuthService>();
                    var user = await authService.GetUserByIdAsync(userId);
                    
                    if (user != null)
                    {
                        context.Session.SetString("UserId", user.Id);
                        context.Session.SetString("Username", user.Username);
                        context.Session.SetInt32("UserRole", (int)user.Role);
                        context.Session.SetString("PasswordChangeRequired", user.IsPasswordChangeRequired ? "true" : "false");
                        
                        if (user.IsPasswordChangeRequired && !path.StartsWith("/ChangePassword", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Redirect("/ChangePassword");
                            return;
                        }
                    }
                    else
                    {
                        context.Response.Redirect("/Login");
                        return;
                    }
                }
                catch
                {
                    // Cookie was tampered with or key is invalid
                    context.Response.Cookies.Delete("Karavul.RememberMe");
                    context.Response.Redirect("/Login");
                    return;
                }
            }
            else
            {
                context.Response.Redirect("/Login");
                return;
            }
        }

        // Şifre değiştirme zorunluysa
        if (!isPublic && context.Session.GetString("PasswordChangeRequired") == "true"
            && !path.StartsWith("/ChangePassword", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/ChangePassword");
            return;
        }

        // Rol tabanlı path koruması
        if (!isPublic)
        {
            var roleInt = context.Session.GetInt32("UserRole") ?? 8;
            var role = (Karavul.Core.Enums.UserRole)roleInt;
            
            bool isAdmin = role.HasFlag(Karavul.Core.Enums.UserRole.Admin);
            bool isEditor = role.HasFlag(Karavul.Core.Enums.UserRole.Editor);

            if (path.StartsWith("/Users", StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith("/Settings", StringComparison.OrdinalIgnoreCase))
            {
                if (!isAdmin)
                {
                    context.Response.Redirect("/");
                    return;
                }
            }

            if (path.StartsWith("/ContactGroups", StringComparison.OrdinalIgnoreCase))
            {
                if (!isAdmin && !isEditor)
                {
                    context.Response.Redirect("/");
                    return;
                }
            }

            if (path.StartsWith("/Monitors/Create", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Monitors/Edit", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Monitors/Delete", StringComparison.OrdinalIgnoreCase))
            {
                if (!isAdmin && !isEditor)
                {
                    context.Response.Redirect("/Monitors");
                    return;
                }
            }
        }

        await next();
    });

    app.Map("/ws/realtime", async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var ws = await context.WebSockets.AcceptWebSocketAsync();
            var realtimeService = context.RequestServices.GetRequiredService<Karavul.Host.Services.RealtimeGraphService>();
            await realtimeService.HandleConnectionAsync(ws);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    });

    app.MapRazorPages();
    app.MapKaravulHealthChecks();

    Log.Information("Karavul http://127.0.0.1:{Port} adresinde dinleniyor.", settings.WebPort);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException && ex.GetType().Name != "HostAbortedException")
{
    Log.Fatal(ex, "Karavul başlatılamadı.");
}
finally
{
    Log.CloseAndFlush();
}


