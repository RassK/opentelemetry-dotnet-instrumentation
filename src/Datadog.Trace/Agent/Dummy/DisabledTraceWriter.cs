using System;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent.Dummy
{
    internal class DisabledTraceWriter : ITraceWriter
    {
        public Task FlushAndCloseAsync()
        {
#if NET452
            return Task.FromResult(true);
#else
            return Task.CompletedTask;
#endif
        }

        public Task FlushTracesAsync()
        {
#if NET452
            return Task.FromResult(true);
#else
            return Task.CompletedTask;
#endif
        }

        public Task<bool> Ping()
        {
            return Task.FromResult(true);
        }

        public void WriteTrace(Span[] trace)
        {
        }
    }
}
