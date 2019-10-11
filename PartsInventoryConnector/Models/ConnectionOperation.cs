// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PartsInventoryConnector.Models
{
    public enum ConnectionOperationStatus
    {
        Unspecified,
        InProgress,
        Completed,
        Failed
    }
    public class ConnectionOperation
    {
        public ErrorDetail Error {get; set; }
        public string Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ConnectionOperationStatus Status { get; set; }
    }
}