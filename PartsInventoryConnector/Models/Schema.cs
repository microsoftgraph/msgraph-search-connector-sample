// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;

namespace PartsInventoryConnector.Models
{
    public class Schema
    {
        public string BaseType { get; set; }
        public List<Property> Properties { get; set; }
    }
}