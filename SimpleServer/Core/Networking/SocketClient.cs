using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FluffyByte.Utilities;
using SimpleServer.Core.Timing;
using SimpleServer.Core.GamePlay;
using SimpleServer.Core.GamePlay.Input;
using SimpleServer.Core.Networking;

namespace FluffyByte.SimpleServer.Core.Networking
{
    /// <summary>
    /// Handles both TCP and UDP communication with a connected client, following the same polling model as SimpleClient.
    /// </summary>
    internal class SocketClient : ITickable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public IPEndPoint? UdpEndPoint { get; set; }
        public TcpClient TcpClient { get; }
        public Socket UdpSocket { get; }

        private readonly NetworkStream _tcpStream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly ConcurrentQueue<string> _tcpInputBuffer = new();
        private readonly GameLoop _gameLoop;

        private bool _disconnect = false;
        public bool IsDisconnecting => _disconnect;

        public string Name { get; private set; } = "SocketClient";
        public string UserName = string.Empty;

        public LoginSession? Login { get; private set; } = new();
        private LoginState? _lastPromptedLoginState = null;
        private bool _hasBeenWelcomed = false;

        public bool IsAuthorized => Login?.State == LoginState.Authorized;

        public event Action<SocketClient>? Disconnected;
        public event Action<SocketClient>? NewConnection;
        public event Action<SocketClient>? MessageReceived;
        public event Action<SocketClient>? MessageSent;
        public event Action<SocketClient>? Authorized;

        public SocketClient(TcpClient tcpClient, Socket udpSocket, GameLoop gameLoop)
        {
            TcpClient = tcpClient;
            UdpSocket = udpSocket;
            _tcpStream = TcpClient.GetStream();
            _reader = new StreamReader(_tcpStream);
            _writer = new StreamWriter(_tcpStream) { AutoFlush = true };
            _gameLoop = gameLoop;

            NewConnection?.Invoke(this);
        }

        public void SetName(string name) => Name = name;

        public bool Connected()
        {
            if (TcpClient.Connected || IsSocketAlive())
                return true;
            else
            {
                _ = TryDisconnect();
                return false;
            }
        }

        public bool IsSocketAlive()
        {
            try
            {
                if (TcpClient == null || !TcpClient.Connected) return false;
                if (TcpClient.Client.Poll(0, SelectMode.SelectRead) && TcpClient.Client.Available == 0) return false;
                return true;
            }
            catch (SocketException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public async Task TryDisconnect()
        {
            if (_disconnect) return;

            _disconnect = true;

            try
            {
                Scribe.Warn($"SocketClient: {Name} received a disconnect request.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
            finally
            {
                PlayerEntity? pe = PlayerManager.TryGetPlayer(UserName);
                if (pe != null) await PlayerManager.UnregisterPlayer(pe);

                _tcpStream.Close();
                _reader.Close();
                _writer.Close();
                _reader.Dispose();
                _writer.Dispose();
                _tcpStream.Dispose();

                Disconnected?.Invoke(this);
            }

            await Task.CompletedTask;
        }

        public async Task<string> ReadMessage()
        {
            try
            {
                if (Connected())
                {
                    string? response = await _reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(response))
                    {
                        MessageReceived?.Invoke(this);
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Warn($"[SocketClient] Read error on {Name}: {ex.Message}");
                await TryDisconnect();
            }

            return string.Empty;
        }

        public async Task SendMessage(string message)
        {
            try
            {
                if (Connected())
                {
                    await _writer.WriteLineAsync(message);
                    MessageSent?.Invoke(this);
                }
            }
            catch (Exception ex)
            {
                Scribe.Warn($"[SocketClient] Write error on {Name}: {ex.Message}");
                await TryDisconnect();
            }
        }

        public void HandleUdp(byte[] data, IPEndPoint senderEndPoint)
        {
            if (UdpEndPoint == null)
            {
                UdpEndPoint = senderEndPoint;
                Scribe.Debug($"[SocketClient {Id}] Registered UDP endpoint: {UdpEndPoint}");
            }

            string message = Encoding.UTF8.GetString(data);
            Scribe.Debug($"[SocketClient {Id}] Received UDP: {message.Trim()}");

            // TODO: Handle game state updates
        }

        public async Task Tick()
        {
            try
            {
                if (!Connected()) return;

                if (!IsAuthorized)
                {
                    await ProcessLogin();
                    return;
                }

                if (!_hasBeenWelcomed)
                {
                    _hasBeenWelcomed = true;
                    _ = Warden.OnAuthorized(this);
                    _ = _gameLoop.RegisterClientAsync(this);
                }

                while (_tcpInputBuffer.TryDequeue(out var input))
                {
                    PlayerEntity? player = PlayerManager.TryGetPlayer(Name);

                    if (player != null)
                    {
                        bool wasCommand = await CommandParser.TryParseAndExecute(player, input);

                        if (!wasCommand)
                            await SendMessage("I don't recognize that command.");
                    }
                    else
                    {
                        await SendMessage("You're not registered in the game.");
                        await TryDisconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        private async Task ProcessLogin()
        {
            if (Login is null || Login.IsCompleted)
                return;

            if (_tcpInputBuffer.TryDequeue(out var loginInput))
            {
                var result = await Login.ProcessInput(this, loginInput);
                if (!string.IsNullOrWhiteSpace(result.Message))
                    await SendMessage(result.Message);

                if (result.Rejected) return;
                if (result.RePrompt) _lastPromptedLoginState = null;
                return;
            }

            if (_lastPromptedLoginState != Login?.State)
            {
                _lastPromptedLoginState = Login?.State;
                if (Login != null)
                    await SendMessage(Login.GetPrompt());
            }
        }

        public void EnqueueInput(string input)
        {
            _tcpInputBuffer.Enqueue(input);
        }

        public async Task BeginInputReadLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && Connected())
                {
                    string input = await ReadMessage();
                    if (!string.IsNullOrWhiteSpace(input))
                        EnqueueInput(input);
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task ToggleAuthorized(string username)
        {
            try
            {
                UserName = username;
                PlayerEntity newPlayer = new(this);
                Authorized?.Invoke(this);
                await PlayerManager.RegisterPlayer(newPlayer);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }
    }
}