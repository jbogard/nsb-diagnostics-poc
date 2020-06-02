using System;
using NServiceBus.Diagnostics.OpenTelemetry.Implementation;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry
{
    public class NServiceBusSendAdapter : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public NServiceBusSendAdapter(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new SendMessageListener("NServiceBus.Extensions.Diagnostics.OutgoingPhysicalMessage", tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose() 
            => _diagnosticSourceSubscriber?.Dispose();
    }
}