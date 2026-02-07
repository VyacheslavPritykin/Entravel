using Entravel.ServiceDefaults;
using MassTransit;
using OrderProcessing.API.HostedServices;
using OrderProcessing.API.MessageConsumers;
using OrderProcessing.API.Observability;
using Serilog;
using Serilog.Events;

Console.WriteLine($"{DateTime.UtcNow:s} Starting...");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.AddServiceDefaults();
    builder.AddNpgsqlDbContext<AppDbContext>("orderdb");
    
    ConfigureLogging(builder);
    ConfigureMessaging(builder);
    ConfigureMetricsAndTracing(builder);
    ConfigureOptions(builder);
    
    builder.Services.AddOrderProcessingAPI();
    builder.Services.AddHostedService<OutboxHostedService>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (!builder.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.UseSerilogRequestLogging();

    app.MapControllers();
    app.MapDefaultEndpoints();

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

return;

void ConfigureLogging(WebApplicationBuilder builder) =>
    builder.Host.UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("app", "OrderProcessing")
        .Enrich.WithProperty("environment", builder.Environment.EnvironmentName)
        .WriteTo.Console(context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Fatal)
        .WriteTo.Seq(context.Configuration.GetConnectionString("seq") ?? "http://localhost:5341"));

void ConfigureMetricsAndTracing(WebApplicationBuilder builder) =>
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddMeter(OrderMetrics.MeterName);
            metrics.AddMeter(MassTransit.Monitoring.InstrumentationOptions.MeterName);
        })
        .WithTracing(tracing =>
        {
            tracing.AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName);
        });

void ConfigureMessaging(WebApplicationBuilder builder) =>
    builder.Services.AddMassTransit(cfg =>
    {
        cfg.AddConsumer<OrderCreatedConsumer>();

        cfg.UsingRabbitMq((context, rabbitCfg) =>
        {
            var connectionString = context.GetRequiredService<IConfiguration>().GetConnectionString("messaging");
            if (!string.IsNullOrEmpty(connectionString))
                rabbitCfg.Host(new Uri(connectionString));

            rabbitCfg.ConfigureEndpoints(context);
        });
    });

void ConfigureOptions(WebApplicationBuilder builder)
{
    builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(nameof(OutboxOptions)));
}