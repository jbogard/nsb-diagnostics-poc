using System;
using OpenTelemetry.Trace.Configuration;

namespace MongoDB.Driver.Core.Extensions.OpenTelemetry
{
    public static class TraceBuilderExtensions
    {
        public static TracerBuilder AddMongoDBAdapter(this TracerBuilder builder)
            => builder
                .AddAdapter(t => new MongoDBCommandAdapter(t));

    }
}
