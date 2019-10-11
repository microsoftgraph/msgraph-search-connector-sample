// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using PartsInventoryConnector.Authentication;
using PartsInventoryConnector.Console;
using PartsInventoryConnector.Graph;
using PartsInventoryConnector.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PartsInventoryConnector
{
    class Program
    {
        private static GraphHelper _graphHelper;

        private static Connection _currentConnection;

        static void Main(string[] args)
        {
            try
            {
                Output.WriteLine("Parts Inventory Search Connector\n");

                // Load configuration from appsettings.json
                var appConfig = LoadAppSettings();
                if (appConfig == null)
                {
                    Output.WriteLine(Output.Error, "Missing or invalid appsettings.json!");
                    Output.WriteLine(Output.Error, "Please see README.md for instructions on creating appsettings.json.");
                    return;
                }

                // Initialize the auth provider
                var authProvider = new ClientCredentialAuthProvider(
                    appConfig["appId"],
                    appConfig["tenantId"],
                    appConfig["appSecret"]
                );

                _graphHelper = new GraphHelper(authProvider);

                do
                {
                    var userChoice = DoMenuPrompt();

                    switch (userChoice)
                    {
                        case MenuChoice.CreateConnection:
                            CreateConnectionAsync().Wait();
                            break;
                        case MenuChoice.ChooseExistingConnection:
                            SelectExistingConnectionAsync().Wait();
                            break;
                        case MenuChoice.DeleteConnection:
                            DeleteCurrentConnectionAsync().Wait();
                            break;
                        case MenuChoice.RegisterSchema:
                            RegisterSchemaAsync().Wait();
                            break;
                        case MenuChoice.ViewSchema:
                            GetSchemaAsync().Wait();
                            break;
                        case MenuChoice.PushItems:
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

                if (connections.Items.Count <= 0)
                {
                    Output.WriteLine(Output.Warning, "No connections exist. Please create a new connection.");
                    return;
                }

                Output.WriteLine(Output.Info, "Choose one of the following connections:");
                int menuNumber = 1;
                foreach(var connection in connections.Items)
                {
                    Output.WriteLine($"{menuNumber++}. {connection.Name}");
                }

                Connection selectedConnection = null;

                do
                {
                    try
                    {
                        Output.Write(Output.Info, "Selection: ");
                        var choice = int.Parse(System.Console.ReadLine());

                        if (choice > 0 && choice <= connections.Items.Count)
                        {
                            selectedConnection = connections.Items[choice-1];
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
                    BaseType = "microsoft.graph.externalItem",
                    Properties = new List<Property>
                    {
                        new Property { Name = "title", Type = Property.StringProperty, IsQueryable = false, IsSearchable = true, IsRetrievable = true },
                        new Property { Name = "priority", Type = Property.IntProperty, IsQueryable = true, IsSearchable = false, IsRetrievable = true },
                        new Property { Name = "assignee", Type = Property.StringProperty, IsQueryable = true, IsSearchable = true, IsRetrievable = true }
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

        private static MenuChoice DoMenuPrompt()
        {
            Output.WriteLine(Output.Info, $"Current connection: {(_currentConnection == null ? "NONE" : _currentConnection.Name)}");
            Output.WriteLine(Output.Info, "Please choose one of the following options:");

            Output.WriteLine($"{Convert.ToInt32(MenuChoice.CreateConnection)}. Create a connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.ChooseExistingConnection)}. Select an existing connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.DeleteConnection)}. Delete current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.RegisterSchema)}. Register schema for current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.ViewSchema)}. View schema for current connection");
            Output.WriteLine($"{Convert.ToInt32(MenuChoice.PushItems)}. Push items to current connection");
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
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
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
