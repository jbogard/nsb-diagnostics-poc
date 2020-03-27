using System;
using NServiceBus.Diagnostics.OpenTelemetry.Implementation;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry
{
    public class NServiceBusReceiveCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public NServiceBusReceiveCollector(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new ProcessMessageListener("NServiceBus.Diagnostics.Receive", tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }


        public void Dispose() 
            => _diagnosticSourceSubscriber?.Dispose();
    }
}