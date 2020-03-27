using System.Diagnostics;
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

            Tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Producer, out var span);
        }

        private void ProcessEvent(Activity activity, AfterSendMessage payload)
        {
            Tracer.CurrentSpan.End();
        }
    }
}