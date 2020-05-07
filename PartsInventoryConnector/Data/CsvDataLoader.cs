using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PartsInventoryConnector.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PartsInventoryConnector.Data
{
    public static class CsvDataLoader
    {
        public static List<AppliancePart> LoadPartsFromCsv(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader))
            {
                csv.Configuration.RegisterClassMap<AppliancePartMap>();
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