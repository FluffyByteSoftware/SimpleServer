using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleServer.Core.Networking;
using SimpleServer.Core.GamePlay.Input;
using SimpleServer.Core.GamePlay.Input.Commands;

namespace SimpleServer.Core.GamePlay.Input
{
    internal static class CommandParser
    {
        private static readonly Dictionary<string, IGameCommand> _commands = [];

        public static async Task<bool> TryParseAndExecute(PlayerEntity player, string input)
        {
            if (input[0] != '/') return false;

            string[] parts = input[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) return false;

            string commandName = parts[0].ToLowerInvariant();
            string[] args = parts.Length > 1 ? parts[1..] : [];

            if(_commands.TryGetValue(commandName, out var command))
            {
                await command.ExecuteAsync(player, args);
                return true;
            }

            await player.WriteLine($"Unknown command: {commandName}");
            
            return true;
        }
    }
}
