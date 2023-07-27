// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// <UsingsSnippet>
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models.ExternalConnectors;
// </UsingsSnippet>
using SdkClientLogging;
using Microsoft.Extensions.Logging;

namespace PartsInventoryConnector.Graph;

public static class GraphHelper
{
    // <GraphInitializationSnippet>
    private static ClientSecretCredential? credential;
    private static GraphServiceClient? graphClient;
    private static HttpClient? httpClient;
    public static void Initialize(Settings settings)
    {
        // Create a credential that uses the client credentials
        // authorization flow
        credential = new ClientSecretCredential(
            settings.TenantId, settings.ClientId, settings.ClientSecret);

        // Create a Graph client using the credential
        // graphClient = new GraphServiceClient(
        //     credential, new[] { "https://graph.microsoft.com/.default" });

        graphClient = GetDebugClient(credential);
    }
    // </GraphInitializationSnippet>

    private static GraphServiceClient GetDebugClient(ClientSecretCredential credential)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .ClearProviders()
                .AddSimpleConsole(options => options.SingleLine = true);
        });
        var logger = loggerFactory.CreateLogger<SdkClientDebugLogMiddleware>();
        var handlers = GraphClientFactory.CreateDefaultHandlers();
        handlers.Add(new SdkClientDebugLogMiddleware(logger, true, true));
        httpClient = GraphClientFactory.Create(handlers);
        return new GraphServiceClient(httpClient,
            new Microsoft.Kiota.Authentication.Azure.AzureIdentityAuthenticationProvider(credential, scopes: new[] { "https://graph.microsoft.com/.default" }));
    }

    // <CreateConnectionSnippet>
    public static async Task<ExternalConnection?> CreateConnectionAsync(string id, string name, string? description)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");

        var newConnection = new ExternalConnection
        {
            Id = id,
            Name = name,
            Description = description,
        };

        return await graphClient.External.Connections.PostAsync(newConnection);
    }
    // <CreateConnectionSnippet>

    // <GetConnectionsSnippet>
    public static async Task<ExternalConnectionCollectionResponse?> GetExistingConnectionsAsync()
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");

        return await graphClient.External.Connections.GetAsync();
    }
    // </GetConnectionsSnippet>

    // <DeleteConnectionSnippet>
    public static async Task DeleteConnectionAsync(string? connectionId)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");
        _ = connectionId ?? throw new ArgumentException("connectionId is required");

        await graphClient.External.Connections[connectionId].DeleteAsync();
    }
    // </DeleteConnectionSnippet>

    // <RegisterSchemaSnippet>
    public static async Task RegisterSchemaAsync(string? connectionId, Schema schema)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");
        _ = credential ?? throw new MemberAccessException("credential is null");
        _ = httpClient ?? throw new MemberAccessException("httpClient is null");
        _ = connectionId ?? throw new ArgumentException("connectionId is required");
        // Use the Graph SDK's request builder to generate the request URL
        var requestInfo = graphClient.External
            .Connections[connectionId]
            .Schema
            .ToGetRequestInformation();

        requestInfo.SetContentFromParsable(graphClient.RequestAdapter, "application/json", schema);

        // Convert the SDK request to an HttpRequestMessage
        var requestMessage = await graphClient.RequestAdapter
            .ConvertToNativeRequestAsync<HttpRequestMessage>(requestInfo);
        _ = requestMessage ?? throw new Exception("Could not create native HTTP request");
        requestMessage.Method = HttpMethod.Post;
        requestMessage.Headers.Add("Prefer", "respond-async");

        // Send the request
        var responseMessage = await httpClient.SendAsync(requestMessage) ??
            throw new Exception("No response returned from API");

        if (responseMessage.IsSuccessStatusCode)
        {
            // The operation ID is contained in the Location header returned
            // in the response
            var operationId = responseMessage.Headers.Location?.Segments.Last() ??
                throw new Exception("Could not get operation ID from Location header");
            await WaitForOperationToCompleteAsync(connectionId, operationId);
        }
        else
        {
            throw new ServiceException("Registering schema failed",
                responseMessage.Headers, (int)responseMessage.StatusCode);
        }
    }

    private static async Task WaitForOperationToCompleteAsync(string connectionId, string operationId)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");

        do
        {
            var operation = await graphClient.External
                .Connections[connectionId]
                .Operations[operationId]
                .GetAsync();

            if (operation?.Status == ConnectionOperationStatus.Completed)
            {
                return;
            }
            else if (operation?.Status == ConnectionOperationStatus.Failed)
            {
                throw new ServiceException($"Schema operation failed: {operation?.Error?.Code} {operation?.Error?.Message}");
            }

            // Wait 5 seconds and check again
            await Task.Delay(5000);
        } while (true);
    }
    // </RegisterSchemaSnippet>

    // <GetSchemaSnippet>
    public static async Task<Schema?> GetSchemaAsync(string? connectionId)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");
        _ = connectionId ?? throw new ArgumentException("connectionId is null");

        return await graphClient.External
            .Connections[connectionId]
            .Schema
            .GetAsync();
    }
    // </GetSchemaSnippet>

    // <AddOrUpdateItemSnippet>
    public static async Task AddOrUpdateItemAsync(string? connectionId, ExternalItem item)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");
        _ = connectionId ?? throw new ArgumentException("connectionId is null");

        await graphClient.External
            .Connections[connectionId]
            .Items[item.Id]
            .PutAsync(item);
    }
    // </AddOrUpdateItemSnippet>

    // <DeleteItemSnippet>
    public static async Task DeleteItemAsync(string? connectionId, string? itemId)
    {
        _ = graphClient ?? throw new MemberAccessException("graphClient is null");
        _ = connectionId ?? throw new ArgumentException("connectionId is null");
        _ = itemId ?? throw new ArgumentException("itemId is null");

        await graphClient.External
            .Connections[connectionId]
            .Items[itemId]
            .DeleteAsync();
    }
    // </DeleteItemSnippet>
}

