using System;
using System.Collections.Generic;
using NServiceBus.Settings;
using NServiceBus.Transport;
using OpenTelemetry.Trace;

namespace NServiceBus.Diagnostics.OpenTelemetry.Implementation
{
    public static class SpanContextExtensions
    {
        public static void ApplyContext(this TelemetrySpan span, ReadOnlySettings settings,
            IReadOnlyDictionary<string, string> contextHeaders)
        {
            var transportDefinition = settings.Get<TransportDefinition>();
            span.SetAttribute("messaging.system", transportDefinition.GetType().Name.Replace("Transport", null).ToLowerInvariant());
            span.SetAttribute("messaging.destination", settings.LogicalAddress().ToString());
            if (contextHeaders.TryGetValue(Headers.ConversationId, out var conversationId))
            {
                span.SetAttribute("messaging.conversation_id", conversationId);
            }

            if (contextHeaders.TryGetValue(Headers.MessageIntent, out var intent) 
                && Enum.TryParse<MessageIntentEnum>(intent, out var intentValue))
            {
                var routingPolicy = settings.Get<TransportInfrastructure>().OutboundRoutingPolicy;

                var kind = GetDestinationKind(intentValue, routingPolicy);

                if (kind != null)
                {
                    span.SetAttribute("messaging.destination_kind", kind);
                }
            }
        }

        private static string GetDestinationKind(MessageIntentEnum intentValue, OutboundRoutingPolicy routingPolicy)
        {
            switch (intentValue)
            {
                case MessageIntentEnum.Send:
                    return ConvertPolicyToKind(routingPolicy.Sends);
                case MessageIntentEnum.Publish:
                    return ConvertPolicyToKind(routingPolicy.Publishes);
                case MessageIntentEnum.Subscribe:
                    return ConvertPolicyToKind(routingPolicy.Sends);
                case MessageIntentEnum.Unsubscribe:
                    return ConvertPolicyToKind(routingPolicy.Sends);
                case MessageIntentEnum.Reply:
                    return ConvertPolicyToKind(routingPolicy.Replies);
                default:
                    return null;
            }
        }

        private static string ConvertPolicyToKind(OutboundRoutingType type)
        {
            switch (type)
            {
                case OutboundRoutingType.Unicast:
                    return "queue";
                case OutboundRoutingType.Multicast:
                    return "topic";
                default:
                    return null;
            }
        }
    }
}