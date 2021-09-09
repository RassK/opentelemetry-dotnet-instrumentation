namespace OpenTelemetry.ClrProfiler.Managed.Instrumentations.MongoDb
{
    internal abstract class MongoDbInstrumentMethodAttribute : InstrumentMethodAttribute
    {
        internal const string MongoDbIntegrationName = "MongoDb";

        internal const string MongoDbClientAssembly = "MongoDB.Driver.Core";

        protected MongoDbInstrumentMethodAttribute(string typeName)
        {
            AssemblyName = MongoDbClientAssembly;
            TypeName = typeName;
            IntegrationName = MongoDbIntegrationName;
        }
    }
}
