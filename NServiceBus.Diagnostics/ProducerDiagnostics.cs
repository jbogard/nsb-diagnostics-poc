using System;
using System.Diagnostics;
using System.Linq;
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

            if (_diagnosticListener.IsEnabled(BeforeSendMessage.EventName, context))
            {
                _diagnosticListener.StartActivity(activity, new BeforeSendMessage(context));
            }
            else
            {
                activity.Start();
            }

            foreach (var header in context.Headers.Where(kvp => kvp.Key.StartsWith("NServiceBus")))
            {
                activity.AddTag(header.Key.Replace("NServiceBus.", ""), header.Value);
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
            _diagnosticListener.StopActivity(activity, new AfterSendMessage(context));
        }
    }

    public class BeforeSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + ".Start";

        public BeforeSendMessage(IOutgoingPhysicalMessageContext context) => Context = context;

        public IOutgoingPhysicalMessageContext Context { get; }
    }

    public class AfterSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + ".Stop";

        public AfterSendMessage(IOutgoingPhysicalMessageContext context) => Context = context;

        public IOutgoingPhysicalMessageContext Context { get; }
    }

}
