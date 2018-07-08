using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;

namespace SheepReaper.Dexter_proto
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();

            new Startup().ConfigureServices(services);

            var process = services.BuildServiceProvider().GetService<PingerProcess>();

            //process.Run();

            for (var i = 0; i < 25; i++)
            {
                new Task(() => process.Run()).Start();
                Thread.Sleep(5000); // Speed limiter because Al Gore thinks I'm ddos
                Console.WriteLine(i);
            }

            LogManager.Shutdown();
            Console.ReadKey();
        }
    }

    public class PingerProcess
    {
        private readonly ILogger<PingerProcess> _logger;
        private readonly List<DexterTarget> _targets;

        public PingerProcess(IOptions<TargetSettings> targetSettings, ILogger<PingerProcess> logger)
        {
            _logger = logger;
            _targets = targetSettings.Value.Targets;
        }

        public bool Run()
        {
            foreach (var target in _targets) RunOnce(target.Name);

            return true;
        }

        public bool RunOnce(string name)
        {
            var pinger = new Ping();

            _logger.LogInformation($"Attempting to Ping the target: {name}");
            try
            {
                var pong = pinger.Send(name);
                var pingable = pong != null && pong.Status == IPStatus.Success;
                return pingable;
            }
            catch (PingException)
            {
                // Discard
                _logger.LogWarning("Ping Exception - Generic Ping error"); //TODO: Add special messages for different ping fails
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Whoops");
                throw;
            }
            finally
            {
                _logger.LogTrace("Cleanup - Disposing pinger");
                pinger.Dispose();
            }
        }
    }
}