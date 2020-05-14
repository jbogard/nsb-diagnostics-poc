using System;
using System.Diagnostics;
using System.Linq;
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
            ProcessEvent(activity, payload as BeforeSendMessage);
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            ProcessEvent(activity, payload as AfterSendMessage);
        }

        private void ProcessEvent(Activity activity, BeforeSendMessage payload)
        {
            if (payload == null)
            {
                AdapterEventSource.Log.NullPayload("SendMessageListener.OnStartActivity");
                return;
            }

            var span = StartSpanFromActivity(activity, payload);

            if (span.IsRecording)
            {
                SetSpanAttributes(activity, payload, span);
            }
        }

        private TelemetrySpan StartSpanFromActivity(Activity activity, BeforeSendMessage payload)
        {
            payload.Context.Headers.TryGetValue(Headers.MessageIntent, out var intent);

            var routes = payload.Context.RoutingStrategies
                .Select(r => r.Apply(payload.Context.Headers))
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

        private static void SetSpanAttributes(Activity activity, BeforeSendMessage payload, TelemetrySpan span)
        {
            span.SetAttribute("messaging.message_id", payload.Context.MessageId);
            span.SetAttribute("messaging.message_payload_size_bytes", payload.Context.Body.Length);

            span.ApplyContext(payload.Context.Builder.Build<ReadOnlySettings>(), payload.Context.Headers);

            foreach (var tag in activity.Tags)
            {
                span.SetAttribute($"messaging.nservicebus.{tag.Key.ToLowerInvariant()}", tag.Value);
            }
        }

        private void ProcessEvent(Activity activity, AfterSendMessage payload)
        {
            Tracer.CurrentSpan.End();
        }
    }
}