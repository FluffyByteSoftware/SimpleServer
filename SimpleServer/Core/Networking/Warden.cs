using System;
using FluffyByte.Utilities;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleServer.Core.Networking
{
    internal static class Warden
    {
        private readonly static CancellationTokenSource _cts = new();
        public static CancellationToken ShutdownToken => _cts.Token;

        public static void Initialize()
        {
            Scribe.Write("[Warden] Online and observing new connections.");
        }

        public static void Shutdown()
        {
            _cts.Cancel();
            Scribe.Write("[Warden] Shutdown complete.");
        }

        /// <summary>
        /// Called immediately after a new connection is accepted by Sentinel.
        /// This is your "first contact" hook with the user.
        /// </summary>
        public static async Task GreetNewClient(SimpleClient client)
        {
            if (!client.Connected()) return;

            await client.SendMessage("Welcome to the server!");
            await client.SendMessage("You must log in before proceeding.");
        }

        /// <summary>
        /// Called the moment a user is authorized. Use this to onboard them or send MOTD.
        /// </summary>
        public static async Task OnAuthorized(SimpleClient client)
        {
            await client.SendMessage("Access granted.");
            await client.SendMessage("Type /help for available commands.");
        }

        /// <summary>
        /// Called when a client is rejected or disconnected due to login failure.
        /// </summary>
        public static async Task OnRejected(SimpleClient client)
        {
            await client.SendMessage("Connection closed. Goodbye.");
        }
    }
}
