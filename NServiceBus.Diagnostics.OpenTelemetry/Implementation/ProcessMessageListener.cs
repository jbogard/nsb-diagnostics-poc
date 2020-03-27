using System.Diagnostics;
using OpenTelemetry.Collector;
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

        }

        public override void OnCustom(string name, Activity activity, object payload)
        {
            switch (name)
            {
                case BeforeProcessMessage.EventName:
                    ProcessEvent(activity, payload as BeforeProcessMessage);
                    break;
                case AfterProcessMessage.EventName:
                    ProcessEvent(activity, payload as AfterProcessMessage);
                    break;
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
                CollectorEventSource.Log.NullPayload("ProcessMessageListener.OnStartActivity");
                return;
            }

            Tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Producer, out var span);
        }

        private void ProcessEvent(Activity activity, AfterSendMessage payload)
        {
            Tracer.CurrentSpan.End();
        }

        private void ProcessEvent(Activity activity, BeforeProcessMessage payload)
        {
            if (payload == null)
            {
                CollectorEventSource.Log.NullPayload("ProcessMessageListener.OnStartActivity");
                return;
            }

            Tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Consumer, out var span);
        }

        private void ProcessEvent(Activity activity, AfterProcessMessage payload)
        {
            Tracer.CurrentSpan.End();
        }
    }
}