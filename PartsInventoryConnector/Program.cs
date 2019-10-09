// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using PartsInventoryConnector.Authentication;
using PartsInventoryConnector.Console;
using System.Net.Http;

namespace PartsInventoryConnector
{
    class Program
    {
        private static HttpClient _graphClient;

        static void Main(string[] args)
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

            // Initialize the Graph client
            // For now, use the HttpClient created by the Graph SDK
            // Once the SDK is updated with the indexing API entities, this
            // can be switched over to using the GraphServiceClient class.
            _graphClient = GraphClientFactory.Create(authProvider, "beta");

            do
            {
                var userChoice = DoMenuPrompt();

                switch (userChoice)
                {
                    case MenuChoice.CreateConnection:
                        break;
                    case MenuChoice.ChooseExistingConnection:
                        break;
                    case MenuChoice.ViewSchema:
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

            } while (true);

        }

        private static MenuChoice DoMenuPrompt()
        {
            return MenuChoice.Invalid;
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
