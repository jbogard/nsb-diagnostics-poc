using OpenTelemetry.Trace.Configuration;

namespace NServiceBus.Diagnostics.OpenTelemetry
{
    public static class TraceBuilderExtensions
    {
        public static TracerBuilder AddNServiceBusAdapter(this TracerBuilder builder) 
            => builder
                .AddAdapter(t => new NServiceBusReceiveAdapter(t))
                .AddAdapter(t => new NServiceBusSendAdapter(t));
    }
}