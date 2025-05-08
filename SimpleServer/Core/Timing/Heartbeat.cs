using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.Utilities;

namespace SimpleServer.Core.Timing
{
    internal class Heartbeat : ICoreService
    {
        public string Name { get; private set; } = "Heartbeat";

        private CancellationTokenSource _cts = new();
        private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(250);
        private Task? _loopTask;

        public event Action? Tick = delegate { };

        private readonly List<ITickable> _subscribers = [];

        public ServiceState Status { get; private set; } = ServiceState.Stopped;

        public CancellationToken ShutdownToken => _cts.Token;

        public Task RequestStart()
        {
            if (Status != ServiceState.Stopped && Status != ServiceState.Failure)
            {
                Scribe.Debug($"[Heartbeat] Cannot start while in state: {Status}.");
                return Task.CompletedTask;
            }

            Scribe.Debug("[Heartbeat] Starting...");

            ResetCancellation();
            Status = ServiceState.Starting;

            _loopTask = Task.Run(() => RunAsync(_cts.Token));
            Status = ServiceState.Running;

            return Task.CompletedTask;
        }

        public async Task RequestRestart()
        {
            Scribe.Debug("[Heartbeat] Restart requested...");

            await RequestStop();

            await Task.Delay(100);
            
            await RequestStart();
        }

        public async Task RequestStop()
        {
            if (Status != ServiceState.Running)
            {
                Scribe.Warn("[Heartbeat] RequestStop() called, but service is not running.");
                return;
            }

            Scribe.Write("[Heartbeat] Stopping...");

            _cts.Cancel();

            if (_loopTask != null)
                await _loopTask;

            Status = ServiceState.Stopped;
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Call all registered subscribers
                    foreach (ITickable sub in _subscribers.ToArray())
                    {
                        try
                        {
                            await sub.Tick();
                        }
                        catch (Exception ex)
                        {
                            Scribe.Error(ex);
                        }
                    }

                    // Call event-based ticks (if any)
                    Tick?.Invoke();
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex);
                }

                try
                {
                    await Task.Delay(_interval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void ResetCancellation()
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        public void Subscribe(ITickable target)
        {
            if (!_subscribers.Contains(target))
                _subscribers.Add(target);
        }

        public void Unsubscribe(ITickable target)
        {
            _subscribers.Remove(target);
        }
    }
}
