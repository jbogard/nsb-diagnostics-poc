using System.Diagnostics;
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
            ProcessEvent(activity, payload as BeforeProcessMessage);
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            ProcessEvent(activity, payload as AfterProcessMessage);
        }

        private void ProcessEvent(Activity activity, BeforeProcessMessage payload)
        {
            if (payload == null)
            {
                AdapterEventSource.Log.NullPayload("ProcessMessageListener.OnStartActivity");
                return;
            }

            var settings = payload.Context.Builder.Build<ReadOnlySettings>();

            var logicalAddress = settings.LogicalAddress();
            var physicalAddress = settings.GetTransportAddress(logicalAddress);
            var localAddress = settings.LocalAddress();
            var instanceSpecificQueue = settings.InstanceSpecificQueue();
            var endpointName = settings.EndpointName();
            var infrastructure = settings.Get<TransportInfrastructure>();
            var definition = settings.Get<TransportDefinition>();
            //var connection = settings.Get("NServiceBus.Transport.Transport");
            // transport connnection string
            
            Tracer.StartActiveSpanFromActivity(settings.LogicalAddress().ToString(), activity, SpanKind.Consumer, out var span);

            if (span.IsRecording)
            {
                span.SetAttribute("messaging.message_id", payload.Context.Message.MessageId);
                span.SetAttribute("messaging.operation", "process");
                span.SetAttribute("messaging.message_payload_size_bytes", payload.Context.Message.Body.Length);

                span.ApplyContext(settings, payload.Context.MessageHeaders);

                foreach (var tag in activity.Tags)
                {
                    span.SetAttribute($"messaging.nservicebus.{tag.Key.ToLowerInvariant()}", tag.Value);
                }
            }
        }

        private void ProcessEvent(Activity activity, AfterProcessMessage payload)
        {
            if (Tracer.CurrentSpan.IsRecording)
            {
                Tracer.CurrentSpan.End();
            }
        }
    }
}