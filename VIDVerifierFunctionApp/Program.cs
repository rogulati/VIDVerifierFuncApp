using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using VIDVerifier;
using VIDVerifier.NotificationCenter;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .Configure<LoggerFilterOptions>(options =>
    {
        LoggerFilterRule? toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

        if (toRemove is not null)
        {
            options.Rules.Remove(toRemove);
        }
    });

builder.Services
    .AddHttpClient()
    .AddSingleton(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    })
    .AddSingleton<AccessTokenProvider>()
    .AddSingleton<RequestCache>()
    .AddSingleton<Configuration>()
    .AddSingleton<INotificationCenter, TeamsNotificationCenter>();

builder.Build().Run();
