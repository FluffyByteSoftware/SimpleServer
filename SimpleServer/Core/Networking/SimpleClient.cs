using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using FluffyByte.Utilities;
using SimpleServer.Core.Timing;
using SimpleServer.Core.Gameplay; // Make sure to include this

namespace SimpleServer.Core.Networking
{
    internal class SimpleClient : ITickable
    {
        public string Name { get; private set; } = "SimpleClient";
        public TcpClient Client { get; private set; }

        private readonly NetworkStream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private LoginState? _lastPromptedLoginState = null;
        private bool _hasBeenWelcomed = false;

        public LoginSession? Login { get; private set; } = new();

        private bool _disconnect = false;
        public bool IsDisconnecting => _disconnect;
        public bool IsAuthorized => Login?.State == LoginState.Authorized;

        private readonly ConcurrentQueue<string> _inputBuffer = new();

        public string UserName = string.Empty;

        public event Action<SimpleClient>? Disconnected;
        public event Action<SimpleClient>? NewConnection;
        public event Action<SimpleClient>? MessageReceived;
        public event Action<SimpleClient>? MessageSent;
        public event Action<SimpleClient>? Authorized;

        private readonly GameLoop _gameLoop;

        public SimpleClient(TcpClient tcpClient, GameLoop gameLoop)
        {
            Client = tcpClient;
            _gameLoop = gameLoop;

            _stream = Client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };

            NewConnection?.Invoke(this);
        }

        public bool Connected()
        {
            if (Client.Connected || IsSocketAlive())
                return true;
            else
            {
                Task.Run(TryDisconnect);
                return false;
            }
        }

        public async Task TryDisconnect()
        {
            if (_disconnect)
                return;

            _disconnect = true;

            try
            {
                Scribe.Warn($"SimpleClient: {Name} has received a disconnect request.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
            finally
            {
                _stream.Close();
                _reader.Close();
                _writer.Close();
                _writer.Dispose();
                _reader.Dispose();
                _stream.Dispose();

                Disconnected?.Invoke(this);
            }

            await Task.CompletedTask;
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
            catch (ObjectDisposedException)
            {
                Scribe.Warn($"[SimpleClient] Tried to write to a disposed stream on {Name}.");
                await TryDisconnect();
            }
            catch (IOException ex)
            {
                Scribe.Warn($"[SimpleClient] IO error during send on {Name}: {ex.Message}");
                await TryDisconnect();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task SendMessageNoNewline(string message)
        {
            try
            {
                if (Connected())
                    await _writer.WriteAsync(message);
                MessageSent?.Invoke(this);
            }
            catch (ObjectDisposedException)
            {
                Scribe.Warn($"[SimpleClient] Tried to write to a disposed stream on {Name}.");
                await TryDisconnect();
            }
            catch (IOException ex)
            {
                Scribe.Warn($"[SimpleClient] IO error during send on {Name}: {ex.Message}");
                await TryDisconnect();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
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
            catch (ObjectDisposedException)
            {
                Scribe.Warn($"[SimpleClient] Tried to read from a disposed stream on {Name}.");
                await TryDisconnect();
            }
            catch (IOException ex)
            {
                Scribe.Warn($"[SimpleClient] IO error during read on {Name}: {ex.Message}");
                await TryDisconnect();
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                await TryDisconnect();
            }

            return string.Empty;
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public bool IsSocketAlive()
        {
            try
            {
                if (Client == null || !Client.Connected)
                    return false;

                if (Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0)
                    return false;

                return true;
            }
            catch (SocketException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public void ToggleAuthorized(string username)
        {
            try
            {
                UserName = username;
                Authorized?.Invoke(this);
                Scribe.Write($"[SimpleClient] {Name} has been authorized.");
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public void EnqueueInput(string input)
        {
            _inputBuffer.Enqueue(input);
        }

        public async Task BeginInputReadLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && Connected())
                {
                    string input = await ReadMessage();

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        EnqueueInput(input);
                    }
                }
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }
        }

        public async Task Tick()
        {
            try
            {
                if (Client == null || !Client.Connected)
                {
                    Scribe.Warn($"[SimpleClient] {Name} is not connected.");
                    return;
                }

                if (!IsSocketAlive())
                {
                    Scribe.Warn($"[SimpleClient] {Name} is not alive.");
                    await Task.Run(TryDisconnect);
                    return;
                }

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

                await HandleGameInputTick();
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

            if (_inputBuffer.TryDequeue(out var loginInput))
            {
                var result = await Login.ProcessInput(this, loginInput);

                if (!string.IsNullOrWhiteSpace(result.Message))
                    await SendMessage(result.Message);

                if (result.Rejected)
                    return;

                if (result.RePrompt)
                    _lastPromptedLoginState = null;

                return;
            }

            if (_lastPromptedLoginState != Login?.State)
            {
                _lastPromptedLoginState = Login?.State;
                if (Login != null)
                    await SendMessage(Login.GetPrompt());
            }
        }

        private async Task HandleGameInputTick()
        {
            while (_inputBuffer.TryDequeue(out var input))
            {
                Scribe.Write($"[SimpleClient] Tick processing: {input}");
                await SendMessageNoNewline($"[Echo] {input}");
            }
        }
    }
}
