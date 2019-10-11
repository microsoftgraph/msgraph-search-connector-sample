// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PartsInventoryConnector.Models
{
    public class GraphCollection<T>
    {
        [JsonProperty("value")]
        public List<T> Items { get; set; }
    }
}