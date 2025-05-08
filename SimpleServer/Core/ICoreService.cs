using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer.Core
{
    public enum ServiceState
    {
        Starting,
        Running,
        Stopped,
        Failure
    }

    internal interface ICoreService
    {
        public ServiceState Status { get; }
        public string Name { get; }

        public CancellationToken ShutdownToken { get; }

        public Task RequestStart();
        public Task RequestRestart();
        public Task RequestStop();
    }
}
