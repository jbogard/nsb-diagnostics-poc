using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Extensions.SystemDiagnostics
{
    public class DiagnosticsActivityEventSubscriber : IEventSubscriber
    {
        public const string ActivityName = "MongoDB.Driver.Core.Events.Command";

        private readonly ReflectionEventSubscriber _subscriber;
        private static readonly DiagnosticSource _diagnosticListener
            = new DiagnosticListener(ActivityName);

        public DiagnosticsActivityEventSubscriber() =>
            _subscriber = new ReflectionEventSubscriber(this, bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler) 
            => _subscriber.TryGetEventHandler(out handler);

        private ConcurrentDictionary<int, Activity> _activityMap = new ConcurrentDictionary<int, Activity>();

        private void Handle(CommandStartedEvent @event)
        {
            var activity = new Activity(ActivityName);

            _diagnosticListener.OnActivityImport(activity, @event);

            if (_diagnosticListener.IsEnabled(CommandStarted.EventName, @event))
            {
                _diagnosticListener.StartActivity(activity, new CommandStarted(@event));
            }
            else
            {
                activity.Start();
            }

            _activityMap.TryAdd(@event.RequestId, activity);
        }

        private void Handle(CommandSucceededEvent @event)
        {
            if (_activityMap.TryRemove(@event.RequestId, out var activity))
            {
                _diagnosticListener.StopActivity(activity, new CommandSucceeded(@event));
            }
        }

        private void Handle(CommandFailedEvent @event)
        {
            if (_activityMap.TryRemove(@event.RequestId, out var activity))
            {
                _diagnosticListener.StopActivity(activity, new CommandFailed(@event));
            }
        }
    }

    public class CommandStarted
    {
        public const string EventName = DiagnosticsActivityEventSubscriber.ActivityName + ".Start";

        public CommandStarted(CommandStartedEvent @event) => Event = @event;

        public CommandStartedEvent Event { get; }
    }

    public class CommandSucceeded
    {
        public const string EventName = DiagnosticsActivityEventSubscriber.ActivityName + ".Stop";

        public CommandSucceeded(CommandSucceededEvent @event) => Event = @event;

        public CommandSucceededEvent Event { get; }
    }

    public class CommandFailed
    {
        public const string EventName = DiagnosticsActivityEventSubscriber.ActivityName + ".Exception";

        public CommandFailed(CommandFailedEvent @event) => Event = @event;

        public CommandFailedEvent Event { get; }
    }
}
