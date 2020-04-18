using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace PartsInventoryConnector.Models
{
    public class PayloadContent
    {
        [JsonProperty]
        public const string Type = "html";
        public string Value { get; set; }
    }
}
