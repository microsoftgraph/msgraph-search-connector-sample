using Microsoft.Graph;
using Microsoft.Graph.ExternalConnectors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace PartsInventoryConnector.Graph
{
    // The Graph SDK serializes enumerations in camelCase.
    // The Graph service currently requires the PropertyType enum
    // to be PascalCase. This will override the Graph serialization
    // If the Graphs service changes to accept camelCase this will no
    // longer be necessary
    class CustomContractResolver : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(PropertyType).IsAssignableFrom(objectType))
            {
                // This default converter uses PascalCase
                return new StringEnumConverter();
            }
            return base.ResolveContractConverter(objectType);
        }
    }

    // In order to hook up the custom contract resolver for
    // PropertyType, we need to implement a custom serializer to
    // pass to the GraphServiceClient.
    public class CustomSerializer : ISerializer
    {

        private Serializer _graphSerializer;
        private JsonSerializerSettings _jsonSerializerSettings;

        public CustomSerializer()
        {
            _graphSerializer = new Serializer();

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CustomContractResolver()
            };
        }

        // For deserialize, just pass through to the default
        // Graph SDK serializer
        public T DeserializeObject<T>(Stream stream)
        {
            return _graphSerializer.DeserializeObject<T>(stream);
        }

        // For deserialize, just pass through to the default
        // Graph SDK serializer
        public T DeserializeObject<T>(string inputString)
        {
            return _graphSerializer.DeserializeObject<T>(inputString);
        }

        public string SerializeObject(object serializeableObject)
        {
            // If a Schema object is being serialized, do the conversion
            // ourselves
            if (serializeableObject is Schema)
            {
                var foo = JsonConvert.SerializeObject(serializeableObject, _jsonSerializerSettings);
                return foo;
            }

            // Otherwise, just pass through to the default Graph SDK serializer
            return _graphSerializer.SerializeObject(serializeableObject);
        }
    }
}