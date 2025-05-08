using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer.Core.Timing
{
    internal interface ITickable
    {
        Task Tick();
    }
}
