using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.API.Auth;
using RVM.LiveBoard.API.Health;
using RVM.LiveBoard.API.Hubs;
using RVM.LiveBoard.API.Middleware;
using RVM.LiveBoard.API.Services;
using RVM.LiveBoard.Infrastructure;
using RVM.LiveBoard.Infrastructure.Data;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter());

        var seqUrl = context.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrEmpty(seqUrl))
            configuration.WriteTo.Seq(seqUrl);
    });

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddSignalR();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
    });

    builder.Services.AddAuthentication(ApiKeyAuthOptions.Scheme)
        .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(ApiKeyAuthOptions.Scheme, options =>
        {
            builder.Configuration.GetSection("ApiKeys").Bind(options);
        });
    builder.Services.AddAuthorization();

    builder.Services.AddScoped<MetricIngestionService>();
    builder.Services.AddHostedService<AlertEvaluationWorker>();
    builder.Services.AddHostedService<MetricCleanupWorker>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<LiveBoardDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    var pathBase = app.Configuration["App:PathBase"];
    if (!string.IsNullOrEmpty(pathBase))
        app.UsePathBase(pathBase);

    app.UseForwardedHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "img-src 'self' data:; " +
            "connect-src 'self' wss:; " +
            "frame-ancestors 'none';";
        await next();
    });

    app.UseStaticFiles();
    app.UseAntiforgery();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorComponents<RVM.LiveBoard.API.Components.App>()
        .AddInteractiveServerRenderMode();
    app.MapHub<LiveMetricHub>("/hubs/live-metrics").AllowAnonymous();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
