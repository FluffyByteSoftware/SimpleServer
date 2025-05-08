using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.Utilities;
using SimpleServer.Core.Networking;

namespace SimpleServer.Core.GamePlay
{
    internal class PlayerEntity(SimpleClient client)
    {
        private readonly SimpleClient _client = client;
        public string Name => _client.Name;

        

        public async Task SendUpdateAsync(string message)
        {
            await _client.SendMessage(message);
        }

        public async Task Tick()
        {
            await _client.SendMessage("Test");
        }

        public async Task Write(string message)
        {
            try
            {
                await _client.SendMessageNoNewline(message);
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
                await _client.SendMessage(message);
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
                string response = await _client.ReadMessage();

                return response;
            }
            catch(Exception ex)
            {
                Scribe.Error(ex);
            }

            return string.Empty;
        }
    }
}
