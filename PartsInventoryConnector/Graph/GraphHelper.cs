// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace PartsInventoryConnector.Graph
{
    public class GraphHelper
    {
        private GraphServiceClient _graphClient;

        public GraphHelper(IAuthenticationProvider authProvider)
        {
            // Initialize the Graph client
            _graphClient = new GraphServiceClient(authProvider);
        }

        #region Connections

        public async Task<ExternalConnection> CreateConnectionAsync(string id, string name, string description)
        {
            var newConnection = new ExternalConnection
            {
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
            var newSchema = await _graphClient.External.Connections[connectionId].Schema
                .Request()
                .Header("Prefer", "respond-async")
                .CreateAsync(schema);

            // TODO: Figure out how to get operation ID
            // Get Location header from response
            await CheckSchemaStatusAsync(connectionId, newSchema.Id);
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
            // TODO: Make sure this does a PUT
            await _graphClient.External.Connections[connectionId]
                .Items[item.Id].Request().CreateAsync(item);
        }

        public async Task<Schema> GetSchemaAsync(string connectionId)
        {
            return await _graphClient.External.Connections[connectionId].Schema.Request().GetAsync();
        }

        #endregion
    }
}