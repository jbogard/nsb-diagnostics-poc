using System;
using OpenTelemetry.Trace.Configuration;

namespace MongoDB.Driver.Core.Extensions.OpenTelemetry
{
    public static class TraceBuilderExtensions
    {
        public static TracerBuilder AddMongoDBCollector(this TracerBuilder builder)
            => builder
                .AddCollector(t => new MongoDBCommandCollector(t));

    }
}
