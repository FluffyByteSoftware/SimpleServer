using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FluffyByte.Utilities;
using SimpleServer.Core.Timing;
using SimpleServer.Core.Gameplay;

namespace SimpleServer.Core.Networking
{
    internal class Sentinel(Heartbeat heartbeat, GameLoop gameLoop) : ICoreService
    {
        public bool IsRunning { get; private set; } = false;
        public CancellationToken ShutdownToken => _cts.Token;

        private CancellationTokenSource _cts = new();
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<SimpleClient, DateTime> _connectedClients = new();

        private readonly Heartbeat _heartbeat = heartbeat;
        private readonly GameLoop _gameLoop = gameLoop;

        public string Name { get; private set; } = "Sentinel";
        public ServiceState Status { get; private set; } = ServiceState.Stopped;

        public event Action<Sentinel>? ShutdownEvent = delegate { };
        public event Action<Sentinel>? StartupEvent = delegate { };

        public async Task RequestStart()
        {
            if (Status == ServiceState.Running || Status == ServiceState.Starting)
                return;

            ResetCancellation();
            await Task.Delay(500); // Give sockets a moment to clear
            
            _ = Start(9998);
        }

        public async Task RequestRestart()
        {
            if (Status != ServiceState.Running)
                Scribe.Warn("[Sentinel] Restart requested but was not running.");

            Scribe.Write("[Sentinel] Restart requested...");

            await RequestStop();
            await Task.Delay(500);
            await RequestStart();
        }

        private async Task Start(int port)
        {
            if (Status != ServiceState.Stopped && Status != ServiceState.Failure)
            {
                Scribe.Warn($"[Sentinel] Cannot start while in state: {Status}.");
                return;
            }

            Status = ServiceState.Starting;
            IsRunning = true;

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();

            Scribe.Write($"[Sentinel] Listening on port {port}...");

            try
            {
                Status = ServiceState.Running;

                StartupEvent?.Invoke(this);

                while (!_cts.Token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                    SimpleClient sClient = new(tcpClient, _gameLoop);


                    sClient.Disconnected += HandleClientDisconnect;

                    _connectedClients.TryAdd(sClient, DateTime.Now);

                    Scribe.Write($"[Sentinel] New client connected: {sClient.Name}");
                    
                    _heartbeat.Subscribe(sClient);
                    
                    _ = sClient.BeginInputReadLoop(_cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Scribe.Warn("[Sentinel] Shutdown requested.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                Status = ServiceState.Failure;
            }
            finally
            {
                _listener?.Stop();
                _listener = null;

                IsRunning = false;
                Status = ServiceState.Stopped;

                Scribe.Write("[Sentinel] Server stopped.");
            }
        }

        public async Task RequestStop()
        {
            if (Status != ServiceState.Running)
            {
                Scribe.Warn("[Sentinel] RequestStop() called, but server is not running.");
                return;
            }

            Scribe.Write("[Sentinel] Stopping...");

            foreach (SimpleClient sClient in _connectedClients.Keys)
            {
                sClient.Disconnected -= HandleClientDisconnect;
                await sClient.TryDisconnect();
            }

            _connectedClients.Clear();

            _cts.Cancel();
            
            ShutdownEvent?.Invoke(this);
        }


        private void HandleClientDisconnect(SimpleClient client)
        {
            if (_connectedClients.TryRemove(client, out _))
            {
                Scribe.Warn($"[Sentinel] Client disconnected: {client.Name} and removed.");
                _heartbeat.Unsubscribe(client);
            }
        }

        private void ResetCancellation()
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}
