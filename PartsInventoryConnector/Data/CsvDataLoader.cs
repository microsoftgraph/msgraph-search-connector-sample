// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// <CsvDataLoaderSnippet>
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PartsInventoryConnector.Data;

public static class CsvDataLoader
{
    public static List<AppliancePart> LoadPartsFromCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<AppliancePartMap>();

        return new List<AppliancePart>(csv.GetRecords<AppliancePart>());
    }
}

public class ApplianceListConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var appliances = text?.Split(';') ?? Array.Empty<string>();
        return new List<string>(appliances);
    }
}

public class AppliancePartMap : ClassMap<AppliancePart>
{
    public AppliancePartMap()
    {
        Map(m => m.PartNumber);
        Map(m => m.Name);
        Map(m => m.Description);
        Map(m => m.Price);
        Map(m => m.Inventory);
        Map(m => m.Appliances).TypeConverter<ApplianceListConverter>();
    }
}
// </CsvDataLoaderSnippet>
