using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleServer.Core.Networking;
using SimpleServer.Core.GamePlay.Input;
using SimpleServer.Core.GamePlay.Input.Commands;
using FluffyByte.Utilities;
using System.Reflection;
using System.Data;

namespace SimpleServer.Core.GamePlay.Input
{
    internal static class CommandParser
    {
        private static readonly Dictionary<string, IGameCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

        static CommandParser()
        {
            LoadCommands();
        }

        private static void LoadCommands()
        {
            var commandTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    typeof(IGameCommand).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && t.IsClass);

            foreach (var type in commandTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is IGameCommand command)
                    {
                        if (!_commands.ContainsKey(command.Name))
                            _commands[command.Name] = command;
                    }
                }
                catch (Exception ex)
                {
                    Scribe.Debug($"[CommandParser] Failed to load command from type {type.Name}: {ex.Message}");
                }
            }
        }

        public static async Task<bool> TryParseAndExecute(PlayerEntity player, string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/'))
                    return false;

                var parts = input[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return false;

                var commandName = parts[0];
                var args = parts.Skip(1).ToArray();

                if (_commands.TryGetValue(commandName, out var command))
                {
                    await command.ExecuteAsync(player, args);
                    return true;
                }

                await player.WriteLine($"Unknown command: {commandName}");
                return true;
            }
            catch (Exception ex)
            {
                Scribe.Error(ex);
                await player.WriteLine("An error occurred while processing your command.");
                return true;
            }
        }
    }

}
