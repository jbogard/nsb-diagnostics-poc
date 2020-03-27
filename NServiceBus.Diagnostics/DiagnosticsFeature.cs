using NServiceBus.Features;

namespace NServiceBus.Diagnostics
{
    public class DiagnosticsFeature : Feature
    {
        public DiagnosticsFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new ConsumerDiagnostics(), "Parses incoming W3C trace information from incoming messages.");
            context.Pipeline.Register(new ProducerDiagnostics(), "Appends W3C trace information to outgoing messages.");
        }
    }
}