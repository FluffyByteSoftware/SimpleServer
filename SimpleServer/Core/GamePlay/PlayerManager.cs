using System.Collections.Concurrent;
using SimpleServer.Core.GamePlay;
using SimpleServer.Core.Networking;
using FluffyByte.Utilities;

namespace SimpleServer.Core.GamePlay
{
    internal static class PlayerManager
    {
        private static readonly FluffyList<PlayerEntity> _players = [];
        
        public static async Task RegisterPlayer(PlayerEntity player)
        {
            if (_players.Contains(player))
            {
                Scribe.Warn("I was asked to register a player that's already registered.");
                return;
            }

            _players.Add(player);
            
            await player.WriteLine("You are registered to the PlayerManager.");

        }

        public static PlayerEntity? TryGetPlayer(string name)
        {
            foreach(PlayerEntity player in _players)
            {
                if (player.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    Scribe.Debug($"Found player {player.Name} in playermanager");
                    return player;
                }
            }

            return null;
        }

        public static async Task UnregisterPlayer(PlayerEntity player)
        {
            if(_players.Contains(player))
            {
                await player.WriteLine("Unregistering you from PlayerManager.");

                _players.Remove(player);
                return;
            }

            Scribe.Debug("I was unable to find player to remove.");
        }

        public static IEnumerable<PlayerEntity> AllPlayers => _players;
    }
}
