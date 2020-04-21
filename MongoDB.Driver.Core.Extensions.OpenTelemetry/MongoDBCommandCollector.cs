using System;
using MongoDB.Driver.Core.Extensions.OpenTelemetry.Implementation;
using MongoDB.Driver.Core.Extensions.SystemDiagnostics;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace MongoDB.Driver.Core.Extensions.OpenTelemetry
{
    public class MongoDBCommandCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public MongoDBCommandCollector(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new CommandListener(DiagnosticsActivityEventSubscriber.ActivityName, tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose()
            => _diagnosticSourceSubscriber?.Dispose();
    }
}