// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// <AppliancePartSnippet>
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Graph.Models.ExternalConnectors;

namespace PartsInventoryConnector.Data;

public class AppliancePart
{
    [JsonPropertyName("appliances@odata.type")]
    private const string AppliancesODataType = "Collection(String)";

    [Key]
    public int PartNumber { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public double Price { get; set; }
    public int Inventory { get; set; }
    public List<string>? Appliances { get; set; }

    public Properties AsExternalItemProperties()
    {
        _ = Name ?? throw new MemberAccessException("Name cannot be null");
        _ = Description ?? throw new MemberAccessException("Description cannot be null");
        _ = Appliances ?? throw new MemberAccessException("Appliances cannot be null");

        var properties = new Properties
        {
            AdditionalData = new Dictionary<string, object>
            {
                { "partNumber", PartNumber },
                { "name", Name },
                { "description", Description },
                { "price", Price },
                { "inventory", Inventory },
                { "appliances@odata.type", "Collection(String)" },
                { "appliances", Appliances }
            }
        };

        return properties;
    }
}
// </AppliancePartSnippet>
