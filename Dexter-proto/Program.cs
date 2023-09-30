using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Hosting;
using SheepReaper.Extensions.DependencyInjection;
using SheepReaper.NETCore.Services;
using SheepReaper.NETCore.Services.Configuration;
using System;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SheepReaper.Dexter_proto
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

            try
            {
                logger.Trace("Building the Host");
                var host = new HostBuilder()
                    //.ConfigureHostConfiguration(configHost => { })
                    .ConfigureAppConfiguration((hostContext, configApp) =>
                    {
                        configApp.AddJsonFile("Appsettings.json", true);
                        configApp.AddEnvironmentVariables();

                        if (args != null) configApp.AddCommandLine(args);
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddOptions();
                        services.ConfigureWritable<TargetSettings>(
                            hostContext.Configuration.GetSection("TargetSettings"));
                        //services.Configure<TargetSettings>(targets =>
                        //    hostContext.Configuration.GetSection("TargetSettings").Bind(targets));
                        services.AddSingleton<IHostedService, PingerService>();
                    })
                    .ConfigureLogging((hostContext, configLogging) =>
                    {
                        configLogging.ClearProviders();
                        configLogging.SetMinimumLevel(LogLevel.Trace);
                    })
                    .UseNLog()
                    .UseConsoleLifetime()
                    .Build();

                using (host)
                {
                    logger.Info("Host Starting... Press CTRL + C to quit.");

                    await host.RunAsync();
                    logger.Info("Stopped");
                }
            }
            catch (Exception ex)
            {
                // Catch setup errors
                logger.Error(ex, "Main execution failed, Exception");
                throw;
            }
            finally
            {
                // Avoid segmentation fault in linux
                LogManager.Shutdown();
            }
        }
    }
}