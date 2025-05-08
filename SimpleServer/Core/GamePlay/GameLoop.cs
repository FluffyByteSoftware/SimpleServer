using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluffyByte.Utilities;
using SimpleServer.Core.GamePlay;
using SimpleServer.Core.Networking;
using SimpleServer.Core.Timing;

namespace SimpleServer.Core.Gameplay
{
    internal class GameLoop : ICoreService, ITickable
    {
        public string Name => "GameLoop";
        public ServiceState Status { get; private set; } = ServiceState.Stopped;
        public CancellationToken ShutdownToken => _cts.Token;

        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, PlayerEntity> _players = new();

        public Task RequestStart()
        {
            Status = ServiceState.Running;
            Scribe.Write("[GameLoop] Started.");
            return Task.CompletedTask;
        }

        public Task RequestRestart()
        {
            _cts.Cancel();
            return RequestStart();
        }

        public Task RequestStop()
        {
            _cts.Cancel();
            Status = ServiceState.Stopped;
            Scribe.Write("[GameLoop] Stopped.");
            return Task.CompletedTask;
        }

        public async Task Tick()
        {
            foreach (PlayerEntity entity in _players.Values)
            {
                try
                {
                    await entity.Tick();
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex);
                }
            }
        }

        public Task RegisterClientAsync(SimpleClient client)
        {
            if (!_players.ContainsKey(client.Name))
            {
                var entity = new PlayerEntity(client);
                _players.TryAdd(client.Name, entity);
                Scribe.Write($"[GameLoop] Registered player: {client.Name}");
            }

            return Task.CompletedTask;
        }

        public Task UnregisterClientAsync(string clientName)
        {
            if (_players.TryRemove(clientName, out var _))
            {
                Scribe.Write($"[GameLoop] Unregistered player: {clientName}");
            }

            return Task.CompletedTask;
        }

        public bool TryGetPlayer(string name, out PlayerEntity? entity)
        {
            return _players.TryGetValue(name, out entity);
        }
    }
}
