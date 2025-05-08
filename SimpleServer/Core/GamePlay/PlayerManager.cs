using System.Collections.Concurrent;
using SimpleServer.Core.GamePlay;
using SimpleServer.Core.Networking;

namespace SimpleServer.Core.GamePlay
{
    internal static class PlayerManager
    {
        private static readonly ConcurrentDictionary<string, PlayerEntity> _players = new();

        public static bool Register(SimpleClient client)
        {
            if (string.IsNullOrEmpty(client.Name) || _players.ContainsKey(client.Name))
                return false;

            _players[client.Name] = new PlayerEntity(client);
            return true;
        }

        public static PlayerEntity? TryGetPlayer(string name)
        {
            foreach(var player in _players.Values)
            {
                if (player.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return player;
            }

            return null;
        }

        public static bool Remove(string name)
        {
            return _players.TryRemove(name, out _);
        }

        public static IEnumerable<PlayerEntity> AllPlayers => _players.Values;
    }
}
