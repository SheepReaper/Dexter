using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SheepReaper.Dexter_proto
{
    public class PingerService : IHostedService, IDisposable
    {
        private readonly ILogger<PingerService> _logger;

        private readonly IWritableOptions<TargetSettings> _options;
        private Timer _timer;
        private int _counter;

        public PingerService(IWritableOptions<TargetSettings> targetSettings, ILogger<PingerService> logger)
        {
            _logger = logger;

            //Test
            _options = targetSettings;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting");

            _timer = new Timer(Run, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping.");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        //Test
        private void DoOtherWork()
        {
            var newDomain = $"MynewDomain{_counter}.com";
            var newTarget = new DexterTarget {Ip = "", Name = newDomain};
            var currTargets = _options.Value.Targets;
            currTargets.Add(newTarget);

            _options.Update(opt => opt.Targets = currTargets);
            _counter++;
        }

        //Test
        //public void Save()
        //{
        //    _config.Value.Targets = _targets;
        //}

        //public string GetTargetsAsJson()
        //{
        //    return JsonConvert.SerializeObject(_targets);
        //}

        //public void AddTarget(DexterTarget newTarget)
        //{
        //    _targets.Add(newTarget);
        //}

        public void Run(object state)
        {
            foreach (var target in _options.Value.Targets) RunOnce(target.Name);

            DoOtherWork();
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
                _logger.LogWarning(
                    "Ping Exception - Generic Ping error"); //TODO: Add special messages for different ping fails
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