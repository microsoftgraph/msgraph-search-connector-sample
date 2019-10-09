// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Graph;

namespace PartsInventoryConnector.Authentication
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(Error error, Exception innerException = null)
            :base(error?.ToString(), innerException)
        {
            this.Error = error;
        }

        public Error Error { get; private set; }
    }
}