namespace NServiceBus.Diagnostics
{
    internal static class Constants
    {
        public const string TraceParentHeaderName = "traceparent";
        public const string TraceStateHeaderName = "tracestate";
        public const string CorrelationContextHeaderName = "correlation-context";
        public const string RequestIdHeaderName = "Request-Id";

        public const string ConsumerActivityName = "NServiceBus.Diagnostics.Receive";
        public const string ProducerActivityName = "NServiceBus.Diagnostics.Send";
    }
}