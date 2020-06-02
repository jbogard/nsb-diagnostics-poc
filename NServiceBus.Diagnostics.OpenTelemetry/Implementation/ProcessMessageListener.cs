using System.Diagnostics;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.Transport;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry.Implementation
{
    internal class ProcessMessageListener : ListenerHandler
    {
        public ProcessMessageListener(string sourceName, Tracer tracer) : base(sourceName, tracer)
        {
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is IIncomingPhysicalMessageContext context))
            {
                AdapterEventSource.Log.NullPayload("ProcessMessageListener.OnStartActivity");
                return;
            }

            var settings = context.Builder.Build<ReadOnlySettings>();

            Tracer.StartActiveSpanFromActivity(settings.LogicalAddress().ToString(), activity, SpanKind.Consumer, out var span);

            if (span.IsRecording)
            {
                span.SetAttribute("messaging.message_id", context.Message.MessageId);
                span.SetAttribute("messaging.operation", "process");
                span.SetAttribute("messaging.message_payload_size_bytes", context.Message.Body.Length);

                span.ApplyContext(settings, context.MessageHeaders);

                foreach (var tag in activity.Tags)
                {
                    span.SetAttribute($"messaging.nservicebus.{tag.Key.ToLowerInvariant()}", tag.Value);
                }
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            if (Tracer.CurrentSpan.IsRecording)
            {
                Tracer.CurrentSpan.End();
            }
        }
    }
}