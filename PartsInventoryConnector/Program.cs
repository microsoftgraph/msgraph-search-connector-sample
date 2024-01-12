// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// <ProgramSnippet>
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Graph.Models.ODataErrors;
using PartsInventoryConnector;
using PartsInventoryConnector.Data;
using PartsInventoryConnector.Graph;

Console.WriteLine("Parts Inventory Search Connector\n");

var settings = Settings.LoadSettings();

// Initialize Graph
InitializeGraph(settings);

ExternalConnection? currentConnection = null;
int choice = -1;

while (choice != 0)
{
    Console.WriteLine($"Current connection: {(currentConnection == null ? "NONE" : currentConnection.Name)}\n");
    Console.WriteLine("Please choose one of the following options:");
    Console.WriteLine("0. Exit");
    Console.WriteLine("1. Create a connection");
    Console.WriteLine("2. Select an existing connection");
    Console.WriteLine("3. Delete current connection");
    Console.WriteLine("4. Register schema for current connection");
    Console.WriteLine("5. View schema for current connection");
    Console.WriteLine("6. Push updated items to current connection");
    Console.WriteLine("7. Push ALL items to current connection");
    Console.Write("Selection: ");

    try
    {
        choice = int.Parse(Console.ReadLine() ?? string.Empty);
    }
    catch (FormatException)
    {
        // Set to invalid value
        choice = -1;
    }

    switch (choice)
    {
        case 0:
            // Exit the program
            Console.WriteLine("Goodbye...");
            break;
        case 1:
            currentConnection = await CreateConnectionAsync();
            break;
        case 2:
            currentConnection = await SelectExistingConnectionAsync();
            break;
        case 3:
            await DeleteCurrentConnectionAsync(currentConnection);
            currentConnection = null;
            break;
        case 4:
            await RegisterSchemaAsync();
            break;
        case 5:
            await GetSchemaAsync();
            break;
        case 6:
            await UpdateItemsFromDatabaseAsync(true, settings.TenantId);
            break;
        case 7:
            await UpdateItemsFromDatabaseAsync(false, settings.TenantId);
            break;
        default:
            Console.WriteLine("Invalid choice! Please try again.");
            break;
    }
}

static string? PromptForInput(string prompt, bool valueRequired)
{
    string? response;

    do
    {
        Console.WriteLine($"{prompt}:");
        response = Console.ReadLine();
        if (valueRequired && string.IsNullOrEmpty(response))
        {
            Console.WriteLine("You must provide a value");
        }
    }
    while (valueRequired && string.IsNullOrEmpty(response));

    return response;
}

static DateTime GetLastUploadTime()
{
    if (File.Exists("lastuploadtime.bin"))
    {
        return DateTime.Parse(
            File.ReadAllText("lastuploadtime.bin")).ToUniversalTime();
    }

    return DateTime.MinValue;
}

static void SaveLastUploadTime(DateTime uploadTime)
{
    File.WriteAllText("lastuploadtime.bin", uploadTime.ToString("u"));
}
// </ProgramSnippet>

// <InitializeGraphSnippet>
void InitializeGraph(Settings settings)
{
    try
    {
        GraphHelper.Initialize(settings);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error initializing Graph: {ex.Message}");
    }
}
// </InitializeGraphSnippet>

// <CreateConnectionSnippet>
async Task<ExternalConnection?> CreateConnectionAsync()
{
    var connectionId = PromptForInput(
        "Enter a unique ID for the new connection (3-32 characters)", true) ?? "ConnectionId";
    var connectionName = PromptForInput(
        "Enter a name for the new connection", true) ?? "ConnectionName";
    var connectionDescription = PromptForInput(
        "Enter a description for the new connection", false);

    try
    {
        // Create the connection
        var connection = await GraphHelper.CreateConnectionAsync(
            connectionId, connectionName, connectionDescription);
        Console.WriteLine(
            "New connection created - Name: {0}, Id: {1}",
            connection?.Name,
            connection?.Id);
        return connection;
    }
    catch (ODataError odataError)
    {
        Console.WriteLine(
            "Error creating connection: {0}: {1} {2}",
            odataError.ResponseStatusCode,
            odataError.Error?.Code,
            odataError.Error?.Message);
        return null;
    }
}
// </CreateConnectionSnippet>

// <GetConnectionsSnippet>
async Task<ExternalConnection?> SelectExistingConnectionAsync()
{
    // TODO
    Console.WriteLine("Getting existing connections...");
    try
    {
        var response = await GraphHelper.GetExistingConnectionsAsync();
        var connections = response?.Value ?? [];
        if (connections.Count <= 0)
        {
            Console.WriteLine("No connections exist. Please create a new connection");
            return null;
        }

        // Display connections
        Console.WriteLine("Choose one of the following connections:");
        var menuNumber = 1;
        foreach (var connection in connections)
        {
            Console.WriteLine($"{menuNumber++}. {connection.Name}");
        }

        ExternalConnection? selection = null;

        do
        {
            try
            {
                Console.Write("Selection: ");
                var choice = int.Parse(Console.ReadLine() ?? string.Empty);
                if (choice > 0 && choice <= connections.Count)
                {
                    selection = connections[choice - 1];
                }
                else
                {
                    Console.WriteLine("Invalid choice.");
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid choice.");
            }
        }
        while (selection == null);

        return selection;
    }
    catch (ODataError odataError)
    {
        Console.WriteLine(
            "Error getting connections: {0}: {1} {2}",
            odataError.ResponseStatusCode,
            odataError.Error?.Code,
            odataError.Error?.Message);
        return null;
    }
}
// </GetConnectionsSnippet>

// <DeleteConnectionSnippet>
async Task DeleteCurrentConnectionAsync(ExternalConnection? connection)
{
    if (connection == null)
    {
        Console.WriteLine(
            "No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    try
    {
        await GraphHelper.DeleteConnectionAsync(connection.Id);
        Console.WriteLine($"{connection.Name} deleted successfully.");
    }
    catch (ODataError odataError)
    {
        Console.WriteLine(
            "Error deleting connection: {0}: {1} {2}",
            odataError.ResponseStatusCode,
            odataError.Error?.Code,
            odataError.Error?.Message);
    }
}
// </DeleteConnectionSnippet>

// <RegisterSchemaSnippet>
async Task RegisterSchemaAsync()
{
    if (currentConnection == null)
    {
        Console.WriteLine(
            "No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    Console.WriteLine("Registering schema, this may take a moment...");

    try
    {
        // Create the schema
        var schema = new Schema
        {
            BaseType = "microsoft.graph.externalItem",
            Properties =
            [
                new() { Name = "partNumber", Type = PropertyType.Int64, IsQueryable = true, IsSearchable = false, IsRetrievable = true, IsRefinable = true },
                new() { Name = "name", Type = PropertyType.String, IsQueryable = true, IsSearchable = true, IsRetrievable = true, IsRefinable = false, Labels = [Label.Title] },
                new() { Name = "description", Type = PropertyType.String, IsQueryable = false, IsSearchable = true, IsRetrievable = true, IsRefinable = false },
                new() { Name = "price", Type = PropertyType.Double, IsQueryable = true, IsSearchable = false, IsRetrievable = true, IsRefinable = true },
                new() { Name = "inventory", Type = PropertyType.Int64, IsQueryable = true, IsSearchable = false, IsRetrievable = true, IsRefinable = true },
                new() { Name = "appliances", Type = PropertyType.StringCollection, IsQueryable = true, IsSearchable = true, IsRetrievable = true, IsRefinable = false },
            ],
        };

        await GraphHelper.RegisterSchemaAsync(currentConnection.Id, schema);
        Console.WriteLine("Schema registered successfully");
    }
    catch (ServiceException serviceException)
    {
        Console.WriteLine(
            "Error registering schema: {0} {1}",
            serviceException.ResponseStatusCode,
            serviceException.Message);
    }
    catch (ODataError odataError)
    {
        Console.WriteLine(
            "Error registering schema: {0}: {1} {2}",
            odataError.ResponseStatusCode,
            odataError.Error?.Code,
            odataError.Error?.Message);
    }
}
// </RegisterSchemaSnippet>

// <GetSchemaSnippet>
async Task GetSchemaAsync()
{
    if (currentConnection == null)
    {
        Console.WriteLine(
            "No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    try
    {
        var schema = await GraphHelper.GetSchemaAsync(currentConnection.Id);
        Console.WriteLine(JsonSerializer.Serialize(schema));
    }
    catch (ODataError odataError)
    {
        Console.WriteLine(
            "Error getting schema: {0}: {1} {2}",
            odataError.ResponseStatusCode,
            odataError.Error?.Code,
            odataError.Error?.Message);
    }
}
// </GetSchemaSnippet>

// <UpdateItemsFromDatabaseSnippet>
async Task UpdateItemsFromDatabaseAsync(bool uploadModifiedOnly, string? tenantId)
{
    if (currentConnection == null)
    {
        Console.WriteLine(
            "No connection selected. Please create a new connection or select an existing connection.");
        return;
    }

    _ = tenantId ?? throw new ArgumentException("tenantId is null");

    List<AppliancePart>? partsToUpload = null;
    List<AppliancePart>? partsToDelete = null;

    var newUploadTime = DateTime.UtcNow;

    var partsDb = new ApplianceDbContext();
    partsDb.EnsureDatabase();

    if (uploadModifiedOnly)
    {
        var lastUploadTime = GetLastUploadTime();
        Console.WriteLine($"Uploading changes since last upload at {lastUploadTime.ToLocalTime()}");

        partsToUpload = partsDb.Parts
            .Where(p => EF.Property<DateTime>(p, "LastUpdated") > lastUploadTime)
            .ToList();

        partsToDelete = partsDb.Parts
            .IgnoreQueryFilters()
            .Where(p => EF.Property<bool>(p, "IsDeleted")
                && EF.Property<DateTime>(p, "LastUpdated") > lastUploadTime)
            .ToList();
    }
    else
    {
        partsToUpload = partsDb.Parts.ToList();

        partsToDelete = partsDb.Parts
            .IgnoreQueryFilters()
            .Where(p => EF.Property<bool>(p, "IsDeleted"))
            .ToList();
    }

    Console.WriteLine($"Processing {partsToUpload.Count} add/updates, {partsToDelete.Count} deletes.");
    var success = true;

    foreach (var part in partsToUpload)
    {
        var newItem = new ExternalItem
        {
            Id = part.PartNumber.ToString(),
            Content = new()
            {
                Type = ExternalItemContentType.Text,
                Value = part.Description,
            },
            Acl =
            [
                new()
                {
                    AccessType = AccessType.Grant,
                    Type = AclType.Everyone,
                    Value = tenantId,
                },
            ],
            Properties = part.AsExternalItemProperties(),
        };

        try
        {
            Console.Write($"Uploading part number {part.PartNumber}...");
            await GraphHelper.AddOrUpdateItemAsync(currentConnection.Id, newItem);
            Console.WriteLine("DONE");
        }
        catch (ODataError odataError)
        {
            success = false;
            Console.WriteLine("FAILED");
            Console.WriteLine(
                "Error: {0}: {1} {2}",
                odataError.ResponseStatusCode,
                odataError.Error?.Code,
                odataError.Error?.Message);
        }
    }

    foreach (var part in partsToDelete)
    {
        try
        {
            Console.Write($"Deleting part number {part.PartNumber}...");
            await GraphHelper.DeleteItemAsync(currentConnection.Id, part.PartNumber.ToString());
            Console.WriteLine("DONE");
        }
        catch (ODataError odataError)
        {
            success = false;
            Console.WriteLine("FAILED");
            Console.WriteLine(
                "Error: {0}: {1} {2}",
                odataError.ResponseStatusCode,
                odataError.Error?.Code,
                odataError.Error?.Message);
        }
    }

    // If no errors, update our last upload time
    if (success)
    {
        SaveLastUploadTime(newUploadTime);
    }
}
// </UpdateItemsFromDatabaseSnippet>
