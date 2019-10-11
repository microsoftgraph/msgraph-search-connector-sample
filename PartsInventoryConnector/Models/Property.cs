// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PartsInventoryConnector.Models
{
    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsQueryable { get; set; }
        public bool IsRetrievable { get; set; }
        public bool IsSearchable { get; set; }

        public static readonly string StringProperty = "String";
        public static readonly string IntProperty = "Int64";
        public static readonly string DoubleProperty = "Double";
        public static readonly string DateTimeProperty = "DateTime";
        public static readonly string BooleanProperty = "Boolean";
        public static readonly string StringCollectionProperty = $"Collection({Property.StringProperty})";
        public static readonly string IntCollectionProperty = $"Collection({Property.IntProperty})";
        public static readonly string DoubleCollectionProperty = $"Collection({Property.DoubleProperty})";
        public static readonly string DateTimeCollectionProperty = $"Collection({Property.DateTimeProperty})";
    }
}