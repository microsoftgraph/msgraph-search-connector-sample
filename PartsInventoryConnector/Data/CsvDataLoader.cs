// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PartsInventoryConnector.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PartsInventoryConnector.Data
{
    public static class CsvDataLoader
    {
        public static List<AppliancePart> LoadPartsFromCsv(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return new List<AppliancePart>(csv.GetRecords<AppliancePart>());
            }
        }
    }

    public class ApplianceListConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var appliances = text.Split(';');
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
}