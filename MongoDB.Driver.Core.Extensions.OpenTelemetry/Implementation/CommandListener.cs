using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using MongoDB.Driver.Core.Extensions.SystemDiagnostics;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;

namespace MongoDB.Driver.Core.Extensions.OpenTelemetry.Implementation
{
    internal class CommandListener : ListenerHandler
    {
        public CommandListener(string sourceName, Tracer tracer) : base(sourceName, tracer)
        {
        }

        private ConcurrentDictionary<int, TelemetrySpan> _spanMap = new ConcurrentDictionary<int, TelemetrySpan>();

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is CommandStarted message))
            {
                CollectorEventSource.Log.NullPayload("CommandListener.OnStartActivity");
                return;
            }

            Tracer.StartActiveSpanFromActivity($"mongodb.{message.Event.CommandName}", activity, SpanKind.Client, out var span);

            span.SetAttribute("db.type", "mongo");
            span.SetAttribute("db.instance", message.Event.DatabaseNamespace.DatabaseName);
            var endPoint = message.Event.ConnectionId?.ServerId?.EndPoint;
            if (endPoint is IPEndPoint ipEndPoint)
            {
                span.SetAttribute("db.user", $"mongodb://{ipEndPoint.Address}:{ipEndPoint.Port}");
                span.SetAttribute("net.peer.ip", ipEndPoint.Address.ToString());
                span.SetAttribute("net.peer.port", ipEndPoint.Port);
            }
            else if (endPoint is DnsEndPoint dnsEndPoint)
            {
                span.SetAttribute("db.user", $"mongodb://{dnsEndPoint.Host}:{dnsEndPoint.Port}");
                span.SetAttribute("net.peer.name", dnsEndPoint.Host);
                span.SetAttribute("net.peer.port", dnsEndPoint.Port);
            }
            span.SetAttribute("db.statement", message.Event.Command.ToString());

            _spanMap.TryAdd(message.Event.RequestId, span);
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            if (!(payload is CommandSucceeded message))
            {
                CollectorEventSource.Log.NullPayload("CommandListener.OnStopActivity");
                return;
            }

            if (_spanMap.TryRemove(message.Event.RequestId, out var span))
            {
                span.End();
            }
        }

        public override void OnException(Activity activity, object payload)
        {
            if (!(payload is CommandFailed message))
            {
                CollectorEventSource.Log.NullPayload("CommandListener.OnExceptionActivity");
                return;
            }

            if (_spanMap.TryRemove(message.Event.RequestId, out var span))
            {
                span.Status = Status.Unknown.WithDescription(message.Event.Failure.Message);
                span.SetAttribute("error.type", message.Event.Failure.GetType().FullName);
                span.SetAttribute("error.msg", message.Event.Failure.Message);
                span.SetAttribute("error.stack", message.Event.Failure.StackTrace);
                span.End();
            }
        }
    }
}