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

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter()));

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
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
        await db.Database.MigrateAsync();
    }

    var pathBase = app.Configuration["App:PathBase"];
    if (!string.IsNullOrEmpty(pathBase))
        app.UsePathBase(pathBase);

    app.UseForwardedHeaders();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
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
