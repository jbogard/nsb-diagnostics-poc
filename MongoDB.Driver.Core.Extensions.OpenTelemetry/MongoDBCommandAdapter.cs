using System;
using MongoDB.Driver.Core.Extensions.OpenTelemetry.Implementation;
using MongoDB.Driver.Core.Extensions.SystemDiagnostics;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace MongoDB.Driver.Core.Extensions.OpenTelemetry
{
    public class MongoDBCommandAdapter : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public MongoDBCommandAdapter(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new CommandListener(DiagnosticsActivityEventSubscriber.ActivityName, tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose()
            => _diagnosticSourceSubscriber?.Dispose();
    }
}