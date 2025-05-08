using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleServer.Core.Networking;

namespace SimpleServer.Core.GamePlay.Input
{
    internal interface IGameCommand
    {
        string Name { get; }
        Task ExecuteAsync(PlayerEntity player, string[] args);
    }
}
