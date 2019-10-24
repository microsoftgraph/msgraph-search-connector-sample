// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PartsInventoryConnector.Models
{
    public class ExternalItem {
        [JsonProperty("@odata.type")]
        private const string oDataType = "microsoft.graph.externalItem";
        public string Id { get; set; }
        public string Content { get; set; }
        public List<Acl> Acl { get; set; }
        public object Properties { get; set; }
    }
}