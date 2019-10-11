// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PartsInventoryConnector.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PartsInventoryConnector.Graph
{
    public class GraphHelper
    {
        private HttpClient _graphClient;
        private JsonSerializerSettings _serializerSettings;

        public GraphHelper(IAuthenticationProvider authProvider)
        {
            // Initialize the Graph client
            // For now, use the HttpClient created by the Graph SDK
            // Once the SDK is updated with the indexing API entities, this
            // can be switched over to using the GraphServiceClient class.
            _graphClient = GraphClientFactory.Create(authProvider, "beta");

            _serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
        }

        #region Connections

        public async Task<Connection> CreateConnectionAsync(string id, string name, string description)
        {
            var newConnection = new Connection
            {
                Id = id,
                Name = name,
                Description = description
            };

            var payload = JsonConvert.SerializeObject(newConnection, _serializerSettings);

            var request = new HttpRequestMessage(HttpMethod.Post, "external/connections");
            request.Content = new StringContent(payload);
            request.Content.Headers.ContentType.MediaType = "application/json";

            var response = await _graphClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var returnedConnection = JsonConvert.DeserializeObject<Connection>(responseJson, _serializerSettings);
                return returnedConnection;
            }

            throw await ExceptionFromResponseAsync(response);
        }

        public async Task<GraphCollection<Connection>> GetExistingConnectionsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "external/connections");
            var response = await _graphClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var returnedCollection =
                    JsonConvert.DeserializeObject<GraphCollection<Connection>>(responseJson, _serializerSettings);
                return returnedCollection;
            }

            throw await ExceptionFromResponseAsync(response);
        }

        public async Task DeleteConnectionAsync(string connectionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"external/connections/{connectionId}");

            var response = await _graphClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw await ExceptionFromResponseAsync(response);
            }
        }

        #endregion

        #region Schema

        public async Task RegisterSchemaAsync(string connectionId, Schema schema)
        {
            var payload = JsonConvert.SerializeObject(schema, _serializerSettings);

            var request = new HttpRequestMessage(HttpMethod.Post, $"external/connections/{connectionId}/schema");
            request.Headers.Add("Prefer", "respond-async");
            request.Content = new StringContent(payload);
            request.Content.Headers.ContentType.MediaType = "application/json";

            var response = await _graphClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Get Location header from response
                await CheckSchemaStatusAsync(response.Headers.Location.AbsoluteUri);
            }

            throw await ExceptionFromResponseAsync(response);
        }

        public async Task CheckSchemaStatusAsync(string operationUri)
        {
            do
            {
                var request = new HttpRequestMessage(HttpMethod.Get, operationUri);
                var response = await _graphClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Get Location header from response
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var operation = JsonConvert.DeserializeObject<ConnectionOperation>(responseBody, _serializerSettings);

                    if (operation.Status == ConnectionOperationStatus.Completed)
                    {
                        return;
                    }
                    else if (operation.Status == ConnectionOperationStatus.Failed)
                    {
                        throw new ServiceException(
                            new Error
                            {
                                Code = operation.Error.ErrorCode,
                                Message = operation.Error.Message
                            }
                        );
                    }

                    await Task.Delay(3000);
                }

                throw await ExceptionFromResponseAsync(response);
            } while (true);
        }

        public async Task<Schema> GetSchemaAsync(string connectionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"external/connections/{connectionId}/schema");
            var response = await _graphClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var returnedSchema = JsonConvert.DeserializeObject<Schema>(responseJson, _serializerSettings);
                return returnedSchema;
            }

            throw await ExceptionFromResponseAsync(response);
        }

        #endregion

        private async Task<ServiceException> ExceptionFromResponseAsync(HttpResponseMessage response)
        {
            var error = new Error{
                Code = "generalException",
                Message = "Unexpected exception returned from the service."
            };

            if (response.Content != null)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                error.Message = responseBody;
            }

            return new ServiceException(error, response.Headers, response.StatusCode);
        }
    }
}