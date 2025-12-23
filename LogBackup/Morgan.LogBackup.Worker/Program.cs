using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morgan.LogBackup;
using Morgan.LogBackup.Application;
using Morgan.LogBackup.Core.Contracts;
using Morgan.LogBackup.Infrastructure.FileSystem;
using Morgan.LogBackup.Infrastructure.Processing;
using Serilog;

Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<ILogSource>(sp =>
            new FileLogSource(
                ctx.Configuration["Paths:Input"],
                sp.GetRequiredService<ILogger<FileLogSource>>()
            ));

        services.AddSingleton<ILogSink>(sp =>
            new FileLogSink(
                ctx.Configuration["Paths:Output"],
                sp.GetRequiredService<ILogger<FileLogSink>>()
            ));

        services.AddSingleton<IOffsetStore>(sp =>
            new JsonOffsetStore(
                ctx.Configuration["Paths:State"],
                sp.GetRequiredService<ILogger<JsonOffsetStore>>()
            ));

        services.AddSingleton<ILogProcessor, JsonLogProcessor>();
        services.AddSingleton<LogBackupRunner>();

        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
