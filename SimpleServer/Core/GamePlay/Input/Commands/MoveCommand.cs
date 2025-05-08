using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SimpleServer.Core.GamePlay.Input.Commands;

namespace SimpleServer.Core.GamePlay.Input.Commands
{
    internal class MoveCommand : IGameCommand
    {
        public string Name => "move";

        public async Task ExecuteAsync(PlayerEntity player, string[] args)
        {
            if (args.Length < 3)
            {
                await player.WriteLine("Usage: /move <x> <y> <z>\nExample: /move 3.5 -2.0 1.0");
                return;
            }

            if (!int.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
            {
                await player.WriteLine($"Invalid X value: {args[0]}");
                return;
            }

            if (!int.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                await player.WriteLine($"Invalid Y value: {args[1]}");
                return;
            }

            if (!int.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                await player.WriteLine($"Invalid Z value: {args[2]}");
                return;
            }

            player.SetPosition(x, y, z);

            await player.WriteLine($"Moved to ({x}, {y}, {z}).");
        }

    }
}
