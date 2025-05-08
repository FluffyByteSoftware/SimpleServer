using System;
using SimpleServer.Core.Networking;
using FluffyByte.Utilities;
using SimpleServer.Core.Timing;
using SimpleServer.Core.GamePlay;

namespace SimpleServer.Core
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if(args.Length > 0)
            {
                Scribe.Write("Arguments provided!");
            }

            Scribe.Write("Hello World!");

            Scribe.Write("Starting SimpleServer...");
            
            Heartbeat heartbeat = new();
            
            GameLoop gLoop = new();
            
            Sentinel sentinel = new(heartbeat, gLoop);
            

            _ = heartbeat.RequestStart();
            
            _ = sentinel.RequestStart(); // Boot her up!

            _ = gLoop.RequestStart();

            heartbeat.Subscribe(gLoop);

            Scribe.Write("SimpleServer started!");

            Scribe.Write("Press any key to exit...");
            
            Console.ReadLine();
            
            Scribe.Write("Exiting SimpleServer...");

            await sentinel.RequestStop();
        }


    }
}