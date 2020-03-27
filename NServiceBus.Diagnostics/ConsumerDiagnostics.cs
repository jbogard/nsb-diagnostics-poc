using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using NServiceBus.Pipeline;

namespace NServiceBus.Diagnostics
{
    public class ConsumerDiagnostics : Behavior<IIncomingPhysicalMessageContext>
    {
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(Constants.ConsumerActivityName);

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var activity = StartActivity(context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private static Activity StartActivity(IIncomingPhysicalMessageContext context)
        {
            var activity = new Activity(Constants.ConsumerActivityName);

            if (!context.MessageHeaders.TryGetValue(Constants.TraceParentHeaderName, out var requestId))
            {
                context.MessageHeaders.TryGetValue(Constants.RequestIdHeaderName, out requestId);
            }

            if (!string.IsNullOrEmpty(requestId))
            {
                activity.SetParentId(requestId);
                if (context.MessageHeaders.TryGetValue(Constants.TraceStateHeaderName, out var traceState))
                {
                    activity.TraceStateString = traceState;
                }

                if (context.MessageHeaders.TryGetValue(Constants.CorrelationContextHeaderName, out var correlationContext))
                {
                    var baggage = correlationContext.Split(',');
                    if (baggage.Length > 0)
                    {
                        foreach (var item in baggage)
                        {
                            if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                            {
                                activity.AddBaggage(baggageItem.Name, HttpUtility.UrlDecode(baggageItem.Value));
                            }
                        }
                    }
                }
            }

            _diagnosticListener.OnActivityImport(activity, context);

            activity.Start();

            if (_diagnosticListener.IsEnabled(BeforeProcessMessage.EventName, context))
            {
                _diagnosticListener.Write(BeforeProcessMessage.EventName, new BeforeProcessMessage(context, activity));
            }

            return activity;
        }

        private static void StopActivity(Activity activity, IIncomingPhysicalMessageContext context)
        {
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }

            if (_diagnosticListener.IsEnabled(AfterProcessMessage.EventName))
            {
                _diagnosticListener.Write(AfterProcessMessage.EventName, new AfterProcessMessage(context, activity));
            }

            activity.Stop();
        }
    }

    public class BeforeProcessMessage
    {
        public const string EventName = Constants.ConsumerActivityName + "." + nameof(BeforeProcessMessage);

        public BeforeProcessMessage(IIncomingPhysicalMessageContext context, Activity activity)
        {
            Context = context;
            StartTimeUtc = activity.StartTimeUtc;
        }

        public IIncomingPhysicalMessageContext Context { get; }
        public DateTime StartTimeUtc { get; }
    }

    public class AfterProcessMessage
    {
        public const string EventName = Constants.ConsumerActivityName + "." + nameof(AfterProcessMessage);

        public AfterProcessMessage(IIncomingPhysicalMessageContext context, Activity activity)
        {
            Context = context;
            StartTimeUtc = activity.StartTimeUtc;
            Duration = activity.Duration;
        }

        public IIncomingPhysicalMessageContext Context { get; }
        public DateTime StartTimeUtc { get; }
        public TimeSpan Duration { get; }
    }
}