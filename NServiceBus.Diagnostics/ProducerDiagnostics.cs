using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Diagnostics
{
    internal class ProducerDiagnostics : Behavior<IOutgoingPhysicalMessageContext>
    {
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(Constants.ProducerActivityName);

        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            var activity = StartActivity(context);

            InjectHeaders(activity, context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private static Activity StartActivity(IOutgoingPhysicalMessageContext context)
        {
            var activity = new Activity(Constants.ProducerActivityName);

            _diagnosticListener.OnActivityImport(activity, context);

            activity.Start();

            if (_diagnosticListener.IsEnabled(BeforeSendMessage.EventName, context))
            {
                _diagnosticListener.Write(BeforeSendMessage.EventName, new BeforeSendMessage(context));
            }

            return activity;
        }

        private static void InjectHeaders(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!context.Headers.ContainsKey(Constants.TraceParentHeaderName))
                {
                    context.Headers[Constants.TraceParentHeaderName] = activity.Id;
                    if (activity.TraceStateString != null)
                    {
                        context.Headers[Constants.TraceStateHeaderName] = activity.TraceStateString;
                    }
                }
            }
            else
            {
                if (!context.Headers.ContainsKey(Constants.RequestIdHeaderName))
                {
                    context.Headers[Constants.RequestIdHeaderName] = activity.Id;
                }
            }
        }

        private static void StopActivity(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }

            if (_diagnosticListener.IsEnabled(AfterSendMessage.EventName))
            {
                _diagnosticListener.Write(AfterSendMessage.EventName, new AfterSendMessage(context));
            }

            activity.Stop();
        }
    }

    public class BeforeSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + "." + nameof(BeforeSendMessage);

        public BeforeSendMessage(IOutgoingPhysicalMessageContext context) => Context = context;

        public IOutgoingPhysicalMessageContext Context { get; }
    }

    public class AfterSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + "." + nameof(AfterSendMessage);

        public AfterSendMessage(IOutgoingPhysicalMessageContext context) => Context = context;

        public IOutgoingPhysicalMessageContext Context { get; }
    }

}
