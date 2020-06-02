using System.Diagnostics;
using System.Linq;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Settings;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry.Implementation
{
    internal class SendMessageListener : ListenerHandler
    {
        public SendMessageListener(string sourceName, Tracer tracer) : base(sourceName, tracer)
        {
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is IOutgoingPhysicalMessageContext context))
            {
                AdapterEventSource.Log.NullPayload("SendMessageListener.OnStartActivity");
                return;
            }

            var span = StartSpanFromActivity(activity, context);

            if (span.IsRecording)
            {
                SetSpanAttributes(activity, context, span);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            Tracer.CurrentSpan.End();
        }

        private TelemetrySpan StartSpanFromActivity(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            context.Headers.TryGetValue(Headers.MessageIntent, out var intent);

            var routes = context.RoutingStrategies
                .Select(r => r.Apply(context.Headers))
                .Select(t =>
                {
                    switch (t)
                    {
                        case UnicastAddressTag u:
                            return u.Destination;
                        case MulticastAddressTag m:
                            return m.MessageType.Name;
                        default:
                            return null;
                    }
                })
                .ToList();

            var operationName = $"{intent ?? activity.OperationName} {string.Join(", ", routes)}";

            Tracer.StartActiveSpanFromActivity(operationName, activity, SpanKind.Producer, out var span);
            return span;
        }

        private static void SetSpanAttributes(Activity activity, IOutgoingPhysicalMessageContext context, TelemetrySpan span)
        {
            span.SetAttribute("messaging.message_id", context.MessageId);
            span.SetAttribute("messaging.message_payload_size_bytes", context.Body.Length);

            span.ApplyContext(context.Builder.Build<ReadOnlySettings>(), context.Headers);

            foreach (var tag in activity.Tags)
            {
                span.SetAttribute($"messaging.nservicebus.{tag.Key.ToLowerInvariant()}", tag.Value);
            }
        }

    }
}