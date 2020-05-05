// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace PartsInventoryConnector.Graph
{
    public class GraphHelper
    {
        private GraphServiceClient _graphClient;

        public GraphHelper(IAuthenticationProvider authProvider)
        {
            // Configure a default HttpProvider with our
            // custom serializer to handle the PropertyType serialization
            var serializer = new CustomSerializer();
            var httpProvider = new HttpProvider(serializer);

            // Initialize the Graph client
            _graphClient = new GraphServiceClient(authProvider, httpProvider);
        }

        #region Connections

        public async Task<ExternalConnection> CreateConnectionAsync(string id, string name, string description)
        {
            var newConnection = new ExternalConnection
            {
                // Need to set to null, service returns 400
                // if @odata.type property is sent
                ODataType = null,
                Id = id,
                Name = name,
                Description = description
            };

            return await _graphClient.External.Connections.Request().AddAsync(newConnection);
        }

        public async Task<IExternalConnectionsCollectionPage> GetExistingConnectionsAsync()
        {
            return await _graphClient.External.Connections.Request().GetAsync();
        }

        public async Task DeleteConnectionAsync(string connectionId)
        {
            await _graphClient.External.Connections[connectionId].Request().DeleteAsync();
        }

        #endregion

        #region Schema

        public async Task RegisterSchemaAsync(string connectionId, Schema schema)
        {
            // Need access to the HTTP response here since we are doing an
            // async request. The new schema object isn't returned, we need
            // the Location header from the response
            var asyncNewSchemaRequest = _graphClient.External.Connections[connectionId].Schema
                .Request()
                .Header("Prefer", "respond-async")
                .GetHttpRequestMessage();

            asyncNewSchemaRequest.Method = HttpMethod.Post;
            asyncNewSchemaRequest.Content = _graphClient.HttpProvider.Serializer.SerializeAsJsonContent(schema);

            var response = await _graphClient.HttpProvider.SendAsync(asyncNewSchemaRequest);

            if (response.IsSuccessStatusCode)
            {
                // Get the operation ID from the Location header
                var operationId = ExtractOperationId(response.Headers.Location);
                await CheckSchemaStatusAsync(connectionId, operationId);
            }
            else
            {
                throw new ServiceException(
                    new Error
                    {
                        Code = response.StatusCode.ToString(),
                        Message = "Registering schema failed"
                    }
                );
            }
        }

        private string ExtractOperationId(System.Uri uri)
        {
            int numSegments = uri.Segments.Length;
            return uri.Segments[numSegments - 1];
        }

        public async Task CheckSchemaStatusAsync(string connectionId, string operationId)
        {
            do
            {
                var operation = await _graphClient.External.Connections[connectionId]
                    .Operations[operationId]
                    .Request()
                    .GetAsync();

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
            } while (true);
        }

        public async Task AddOrUpdateItem(string connectionId, ExternalItem item)
        {
            // The SDK's auto-generated request builder uses POST here,
            // which isn't correct. For now, get the HTTP request and change it
            // to PUT manually.
            var putItemRequest = _graphClient.External.Connections[connectionId]
                .Items[item.Id].Request().GetHttpRequestMessage();

            putItemRequest.Method = HttpMethod.Put;
            putItemRequest.Content = _graphClient.HttpProvider.Serializer.SerializeAsJsonContent(item);

            var response = await _graphClient.HttpProvider.SendAsync(putItemRequest);
            if (!response.IsSuccessStatusCode)
            {
                throw new ServiceException(
                    new Error
                    {
                        Code = response.StatusCode.ToString(),
                        Message = "Error indexing item."
                    }
                );
            }
        }

        public async Task<Schema> GetSchemaAsync(string connectionId)
        {
            return await _graphClient.External.Connections[connectionId].Schema.Request().GetAsync();
        }

        #endregion
    }
}