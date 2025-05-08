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
            if(args.Contains("--debug"))
            {
                DebugFlags.EnableVerboseLogging();
            }

            Scribe.Write("Fetching core services...");
            
            Heartbeat heartbeat = new();
            
            GameLoop gLoop = new();
            
            Sentinel sentinel = new(heartbeat, gLoop);

            Scribe.Write("Initial startup completed.");

            _ = heartbeat.RequestStart();
            
            _ = sentinel.RequestStart(); // Boot her up!

            _ = gLoop.RequestStart();

            Scribe.Write("All core services started.");

            heartbeat.Subscribe(gLoop);

            Scribe.Debug("GameLoop subscribed to Heartbeat.");

            Scribe.Write("Press any key to exit...");
            
            Console.ReadLine();
            
            Scribe.Write("Exiting SimpleServer...");

            await sentinel.RequestStop();
        }


    }
}