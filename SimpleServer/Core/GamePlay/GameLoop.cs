using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluffyByte.SimpleServer.Core.Networking;
using FluffyByte.Utilities;
using SimpleServer.Core.GamePlay;
using SimpleServer.Core.Networking;
using SimpleServer.Core.Timing;

namespace SimpleServer.Core.GamePlay
{
    internal class GameLoop : ICoreService, ITickable
    {
        public string Name => "GameLoop";
        public ServiceState Status { get; private set; } = ServiceState.Stopped;
        public CancellationToken ShutdownToken => _cts.Token;

        private readonly CancellationTokenSource _cts = new();
        private readonly FluffyList<PlayerEntity> _players = new();

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
            foreach (PlayerEntity entity in _players)
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
            _players.Add(new PlayerEntity(client));

            return Task.CompletedTask;
        }

        public Task RegisterClientAsync(SocketClient client)
        {
            try
            {
                
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }
          
        }

        public Task UnregisterClientAsync(string clientName)
        {
            if (_players.TryRemove(clientName, out var _))
            {
                Scribe.Debug($"[GameLoop] Unregistered player: {clientName}");
            }

            return Task.CompletedTask;
        }

        public bool TryGetPlayer(string name, out PlayerEntity? entity)
        {
            return _players.TryGetValue(name, out entity);
        }
    }
}
