// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PartsInventoryConnector.Console
{
    public enum MenuChoice
    {
        Invalid = 0,
        CreateConnection,
        ChooseExistingConnection,
        DeleteConnection,
        RegisterSchema,
        ViewSchema,
        PushUpdatedItems,
        PushAllItems,
        Exit
    }
}