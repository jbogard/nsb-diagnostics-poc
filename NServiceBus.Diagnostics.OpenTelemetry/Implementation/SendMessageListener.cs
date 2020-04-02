using System;
using System.Diagnostics;
using System.Linq;
using NServiceBus.Routing;
using OpenTelemetry.Collector;
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

        }

        public override void OnCustom(string name, Activity activity, object payload)
        {
            switch (name)
            {
                case BeforeSendMessage.EventName:
                    ProcessEvent(activity, payload as BeforeSendMessage);
                    break;
                case AfterSendMessage.EventName:
                    ProcessEvent(activity, payload as AfterSendMessage);
                    break;
            }
        }

        private void ProcessEvent(Activity activity, BeforeSendMessage payload)
        {
            if (payload == null)
            {
                CollectorEventSource.Log.NullPayload("SendMessageListener.OnStartActivity");
                return;
            }

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
                });

            var operationName = $"{intent ?? activity.OperationName} {string.Join(", ", routes)}";

            Tracer.StartActiveSpanFromActivity(operationName, activity, SpanKind.Producer, out var span);

            if (span.IsRecording)
            {
                foreach (var header in payload.Context.Headers.Where(pair =>
                    pair.Key.StartsWith("NServiceBus.", StringComparison.OrdinalIgnoreCase)))
                {
                    span.SetAttribute(header.Key, header.Value);
                }
            }
        }

        private void ProcessEvent(Activity activity, AfterSendMessage payload)
        {
            Tracer.CurrentSpan.End();
        }
    }
}