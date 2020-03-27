using OpenTelemetry.Trace.Configuration;

namespace NServiceBus.Diagnostics.OpenTelemetry
{
    public static class TraceBuilderExtensions
    {
        public static TracerBuilder AddNServiceBusCollector(this TracerBuilder builder) 
            => builder
                .AddCollector(t => new NServiceBusReceiveCollector(t))
                .AddCollector(t => new NServiceBusSendCollector(t));
    }
}