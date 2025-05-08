using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleServer.Core.Networking;
using FluffyByte.Utilities;
using SimpleServer.Core.Networking;

namespace SimpleServer.Core.GamePlay
{
    internal class PlayerEntity
    {
        public SimpleClient? SClient;
        public SocketClient? SoClient;

        public string Name => SClient.Name;

        public Vector3 Position3D { get; private set; } = new(0, 0, 0);

        public PlayerEntity(SimpleClient client)
        {
            SClient = client;
        }

        public PlayerEntity(SocketClient sClient)
        {
            SoClient = sClient;
        }

        public async Task SendUpdateAsync(string message)
        {
            await SClient.SendMessage(message);
        }

        public Task Tick()
        {
            return Task.CompletedTask;
        }

        public async Task Write(string message)
        {
            try
            {
                await SClient.SendMessageNoNewline(message);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }

        }

        public async Task WriteLine(string message)
        {
            try
            {
                await SClient.SendMessage(message);
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
            }

        }

        public async Task<string> ReadLine()
        {
            try
            {
                string response = await SClient.ReadMessage();

                return response;
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }

            return string.Empty;
        }

        public void SetPosition(int x, int y, int z)
        {
            Position3D = new(x, y, z);

            Scribe.Debug($"[PlayerEntity] {Name} moved to {Position3D}");
            //UpdatePosition();
        }
    }
}
