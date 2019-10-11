// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;

namespace PartsInventoryConnector.Models
{
    public class ErrorDetail
    {
        public List<InnerErrorDetail> Details { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }
}