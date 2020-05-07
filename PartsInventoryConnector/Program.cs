// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using PartsInventoryConnector.Authentication;
using PartsInventoryConnector.Console;
using PartsInventoryConnector.Data;
using PartsInventoryConnector.Graph;
using PartsInventoryConnector.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PartsInventoryConnector
{
    class Program
    {
        private static GraphHelper _graphHelper;

        private static ExternalConnection _currentConnection;

        private static string _tenantId;

        static async Task Main(string[] args)
        {
            try
            {
                Output.WriteLine("Parts Inventory Search Connector\n");

                // Load configuration from appsettings.json
                var appConfig = LoadAppSettings();
                if (appConfig == null)
                {
                    Output.WriteLine(Output.Error, "Missing or invalid user secrets");
                    Output.WriteLine(Output.Error, "Please see README.md for instructions on configuring the application.");
                    return;
                }

                // Save tenant ID for setting ACL on items
                _tenantId = appConfig["tenantId"];

                // Initialize the auth provider
                var authProvider = new ClientCredentialAuthProvider(
                    appConfig["appId"],
                    appConfig["tenantId"],
                    appConfig["appSecret"]
                );

                // Check if the database is empty
                using (var db = new ApplianceDbContext())
                {
                    if (db.Parts.IgnoreQueryFilters().Count() <= 0)
                    {
                        Output.WriteLine(Output.Warning, "Database empty, importing entries from CSV file");
                        ImportCsvToDatabase(db, "ApplianceParts.csv");
                    }
                }

                _graphHelper = new GraphHelper(authProvider);

                do
                {
                    var userChoice = DoMenuPrompt();

                    switch (userChoice)
                    {
                        case MenuChoice.CreateConnection:
                            await CreateConnectionAsync();
                            break;
                        case MenuChoice.ChooseExistingConnection:
                            await SelectExistingConnectionAsync();
                            break;
                        case MenuChoice.DeleteConnection:
                            await DeleteCurrentConnectionAsync();
                            break;
                        case MenuChoice.RegisterSchema:
                            await RegisterSchemaAsync();
                            break;
                        case MenuChoice.ViewSchema:
                            await GetSchemaAsync();
                            break;
                        case MenuChoice.PushUpdatedItems:
                            await UpdateItemsFromDatabase(true);
                            break;
                        case MenuChoice.PushAllItems:
                            await UpdateItemsFromDatabase(false);
                            break;
                        case MenuChoice.Exit:
                            // Exit the program
                            Output.WriteLine("Goodbye...");
                            return;
                        case MenuChoice.Invalid:
                        default:
                            Output.WriteLine(Output.Warning, "Invalid choice! Please try again.");
                            break;
                    }

                    Output.WriteLine("");

                } while (true);
            }
            catch (Exception ex)
            {
                Output.WriteLine(Output.Error, "An unexpected exception occurred.");
                Output.WriteLine(Output.Error, ex.Message);
                Output.WriteLine(Output.Error, ex.StackTrace);
            }
        }

        private static async Task CreateConnectionAsync()
        {
            var connectionId = PromptForInput("Enter a unique ID for the new connection", true);
            var connectionName = PromptForInput("Enter a name for the new connection", true);
            var connectionDescription = PromptForInput("Enter a description for the new connection", false);

            try
            {
                // Create the connection
                _currentConnection = await _graphHelper.CreateConnectionAsync(connectionId, connectionName, connectionDescription);
                Output.WriteLine(Output.Success, "New connection created");
                Output.WriteObject(Output.Info, _currentConnection);
            }
            catch (ServiceException serviceException)
            {
                Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error creating new connection:");
                Output.WriteLine(Output.Error, serviceException.Message);
                return;
            }
        }

        private static async Task SelectExistingConnectionAsync()
        {
            Output.WriteLine(Output.Info, "Getting existing connections...");
            try
            {
                // Get connections
                var connections = await _graphHelper.GetExistingConnectionsAsync();

                if (connections.CurrentPage.Count <= 0)
                {
                    Output.WriteLine(Output.Warning, "No connections exist. Please create a new connection.");
                    return;
                }

                Output.WriteLine(Output.Info, "Choose one of the following connections:");
                int menuNumber = 1;
                foreach(var connection in connections.CurrentPage)
                {
                    Output.WriteLine($"{menuNumber++}. {connection.Name}");
                }

                ExternalConnection selectedConnection = null;

                do
                {
                    try
                    {
                        Output.Write(Output.Info, "Selection: ");
                        var choice = int.Parse(System.Console.ReadLine());

                        if (choice > 0 && choice <= connections.CurrentPage.Count)
                        {
                            selectedConnection = connections.CurrentPage[choice-1];
                        }
                        else
                        {
                            Output.WriteLine(Output.Warning, "Invalid choice.");
                        }
                    }
                    catch (FormatException)
                    {
                        Output.WriteLine(Output.Warning, "Invalid choice.");
                    }
                } while (selectedConnection == null);

                _currentConnection = selectedConnection;
            }
            catch (ServiceException serviceException)
            {
                Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error getting connections:");
                Output.WriteLine(Output.Error, serviceException.Message);
                return;
            }
        }

        private static async Task DeleteCurrentConnectionAsync()
        {
            if (_currentConnection == null)
            {
                Output.WriteLine(Output.Warning, "No connection selected. Please create a new connection or select an existing connection.");
                return;
            }

            Output.WriteLine(Output.Warning, $"Deleting {_currentConnection.Name} - THIS CANNOT BE UNDONE");
            Output.WriteLine(Output.Warning, "Enter the connection name to confirm.");

            var input = System.Console.ReadLine();

            if (input != _currentConnection.Name)
            {
                Output.WriteLine(Output.Warning, "Canceled");
            }

            try
            {
                await _graphHelper.DeleteConnectionAsync(_currentConnection.Id);
                Output.WriteLine(Output.Success, $"{_currentConnection.Name} deleted");
                _currentConnection = null;
            }
            catch (ServiceException serviceException)
            {
                Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error deleting connection:");
                Output.WriteLine(Output.Error, serviceException.Message);
                return;
            }
        }

        private static async Task RegisterSchemaAsync()
        {
            if (_currentConnection == null)
            {
                Output.WriteLine(Output.Warning, "No connection selected. Please create a new connection or select an existing connection.");
                return;
            }

            Output.WriteLine(Output.Info, "Registering schema, this may take a moment...");

            try
            {
                // Register the schema
                var schema = new Schema
                {
                    // Need to set to null, service returns 400
                    // if @odata.type property is sent
                    ODataType = null,
                    BaseType = "microsoft.graph.externalItem",
                    Properties = new List<Property>
                    {
                        new Property { Name = "partNumber", Type = PropertyType.Int64, IsQueryable = true, IsSearchable = false, IsRetrievable = true },
                        new Property { Name = "name", Type = PropertyType.String, IsQueryable = true, IsSearchable = true, IsRetrievable = true },
                        new Property { Name = "description", Type = PropertyType.String, IsQueryable = false, IsSearchable = true, IsRetrievable = true },
                        new Property { Name = "price", Type = PropertyType.Double, IsQueryable = true, IsSearchable = false, IsRetrievable = true },
                        new Property { Name = "inventory", Type = PropertyType.Int64, IsQueryable = true, IsSearchable = false, IsRetrievable = true },
                        new Property { Name = "appliances", Type = PropertyType.StringCollection, IsQueryable = true, IsSearchable = true, IsRetrievable = true }
                    }
                };

                await _graphHelper.RegisterSchemaAsync(_currentConnection.Id, schema);
                Output.WriteLine(Output.Success, "Schema registered");
            }
            catch (ServiceException serviceException)
            {
                Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error registering schema:");
                Output.WriteLine(Output.Error, serviceException.Message);
                return;
            }
        }

        private static async Task GetSchemaAsync()
        {
            if (_currentConnection == null)
            {
                Output.WriteLine(Output.Warning, "No connection selected. Please create a new connection or select an existing connection.");
                return;
            }

            try
            {
                var schema = await _graphHelper.GetSchemaAsync(_currentConnection.Id);
                Output.WriteObject(Output.Info, schema);
            }
            catch (ServiceException serviceException)
            {
                Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error getting schema:");
                Output.WriteLine(Output.Error, serviceException.Message);
                return;
            }
        }

        private static void ImportCsvToDatabase(ApplianceDbContext db, string partsFilePath)
        {
            var parts = CsvDataLoader.LoadPartsFromCsv(partsFilePath);
            db.AddRange(parts);
            db.SaveChanges();
        }

        private static async Task UpdateItemsFromDatabase(bool uploadModifiedOnly)
        {
            if (_currentConnection == null)
            {
                Output.WriteLine(Output.Warning, "No connection selected. Please create a new connection or select an existing connection.");
                return;
            }

            List<AppliancePart> partsToUpload = null;
            List<AppliancePart> partsToDelete = null;

            var newUploadTime = DateTime.UtcNow;

            using (var db = new ApplianceDbContext())
            {
                if (uploadModifiedOnly)
                {
                    // Load the last upload timestamp
                    var lastUploadTime = GetLastUploadTime();
                    Output.WriteLine(Output.Info, $"Uploading changes since last upload at {lastUploadTime.ToLocalTime().ToString()}");

                    partsToUpload = db.Parts
                        .Where(p => EF.Property<DateTime>(p, "LastUpdated") > lastUploadTime)
                        .ToList();

                    partsToDelete = db.Parts
                        .IgnoreQueryFilters()
                        .Where(p => (EF.Property<bool>(p, "IsDeleted") && EF.Property<DateTime>(p, "LastUpdated") > lastUploadTime))
                        .ToList();
                }
                else
                {
                    partsToUpload = db.Parts
                        .ToList();

                    partsToDelete = db.Parts
                        .IgnoreQueryFilters()
                        .Where(p => EF.Property<bool>(p, "IsDeleted"))
                        .ToList();
                }
            }

            Output.WriteLine(Output.Info, $"Processing {partsToUpload.Count()} add/updates, {partsToDelete.Count()} deletes");
            bool success = true;

            foreach(var part in partsToUpload)
            {
                var newItem = new ExternalItem
                {
                    Id = part.PartNumber.ToString(),
                    Content = new ExternalItemContent
                    {
                        // Need to set to null, service returns 400
                        // if @odata.type property is sent
                        ODataType = null,
                        Type = ExternalItemContentType.Text,
                        Value = part.Description
                    },
                    Acl = new List<Acl>
                    {
                        new Acl {
                            AccessType = AccessType.Grant,
                            Type = AclType.Everyone,
                            Value = _tenantId,
                            IdentitySource = "Azure Active Directory"
                        }
                    },
                    Properties = part.AsExternalItemProperties()
                };

                try
                {
                    Output.Write(Output.Info, $"Uploading part number {part.PartNumber}...");
                    await _graphHelper.AddOrUpdateItem(_currentConnection.Id, newItem);
                    Output.WriteLine(Output.Success, "DONE");
                }
                catch (ServiceException serviceException)
                {
                    success = false;
                    Output.WriteLine(Output.Error, "FAILED");
                    Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error adding or updating part {part.PartNumber}");
                    Output.WriteLine(Output.Error, serviceException.Message);
                }
            }

            foreach (var part in partsToDelete)
            {
                try
                {
                    Output.Write(Output.Info, $"Deleting part number {part.PartNumber}...");
                    await _graphHelper.DeleteItem(_currentConnection.Id, part.PartNumber.ToString());
                    Output.WriteLine(Output.Success, "DONE");
                }
                catch (ServiceException serviceException)
                {
                    if (serviceException.StatusCode.Equals(System.Net.HttpStatusCode.NotFound))
                    {
                        Output.WriteLine(Output.Warning, "Not found");
                    }
                    else
                    {
                        success = false;
                        Output.WriteLine(Output.Error, "FAILED");
                        Output.WriteLine(Output.Error, $"{serviceException.StatusCode} error deleting part {part.PartNumber}");
                        Output.WriteLine(Output.Error, serviceException.Message);
                    }
                }
            }

            // If no errors, update our last upload time
            if (success)
            {
                SaveLastUploadTime(newUploadTime);
            }
        }

        private static readonly string uploadTimeFile = "lastuploadtime.bin";
        private static DateTime GetLastUploadTime()
        {
            if (System.IO.File.Exists(uploadTimeFile))
            {
                var uploadTimeString = System.IO.File.ReadAllText(uploadTimeFile);
                return DateTime.Parse(uploadTimeString).ToUniversalTime();
            }

            return DateTime.MinValue;
        }

        private static void SaveLastUploadTime(DateTime uploadTime)
        {
            System.IO.File.WriteAllText(uploadTimeFile, uploadTime.ToString("u"));
        }

        private static MenuChoice DoMenuPrompt()
        {
            Output.WriteLine(Output.Info, $"Current connection: {(_currentConnection == null ? "NONE" : _currentConnection.Name)}");
            Output.WriteLine(Output.Info, "Please choose one of the following options:");

            Output.WriteLine($"{Convert.ToInt32(MenuChoice.CreateConnection)}. Create a connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.ChooseExistingConnection)}. Select an existing connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.DeleteConnection)}. Delete current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.RegisterSchema)}. Register schema for current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.ViewSchema)}. View schema for current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.PushUpdatedItems)}. Push updated items to current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.PushAllItems)}. Push ALL items to current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.Exit)}. Exit");

            try
            {
                var choice = int.Parse(System.Console.ReadLine());
                return (MenuChoice)choice;
            }
            catch (FormatException)
            {
                return MenuChoice.Invalid;
            }
        }

        private static string PromptForInput(string prompt, bool valueRequired)
        {
            string response = null;

            do
            {
                Output.WriteLine(Output.Info, $"{prompt}:");
                response = System.Console.ReadLine();
                if (valueRequired && string.IsNullOrEmpty(response))
                {
                    Output.WriteLine(Output.Error, "You must provide a value");
                }
            } while (valueRequired && string.IsNullOrEmpty(response));

            return response;
        }

        private static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["appId"]) ||
                string.IsNullOrEmpty(appConfig["appSecret"]) ||
                string.IsNullOrEmpty(appConfig["tenantId"]))
            {
                return null;
            }

            return appConfig;
        }
    }
}
