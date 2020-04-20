using System;
using NServiceBus.Diagnostics.OpenTelemetry.Implementation;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry
{
    public class NServiceBusSendCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public NServiceBusSendCollector(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new SendMessageListener("NServiceBus.Diagnostics.Send", tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose() 
            => _diagnosticSourceSubscriber?.Dispose();
    }
}