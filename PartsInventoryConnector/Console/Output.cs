// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;
using System;

namespace PartsInventoryConnector.Console
{
    class Output
    {
        public const ConsoleColor Default = ConsoleColor.White;
        public const ConsoleColor Info = ConsoleColor.Cyan;
        public const ConsoleColor Error = ConsoleColor.Red;
        public const ConsoleColor Warning = ConsoleColor.Yellow;
        public const ConsoleColor Success = ConsoleColor.Green;

        public static void Write(string output)
        {
            System.Console.Write(output);
        }

        public static void Write(ConsoleColor color, string output)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write(output);
            System.Console.ResetColor();
        }

        public static void Write(string format, params object[] values)
        {
            System.Console.Write(format, values);
        }

        public static void Write(ConsoleColor color, string format, params object[] values)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write(format, values);
            System.Console.ResetColor();
        }

        public static void WriteLine(string output)
        {
            System.Console.WriteLine(output);
        }

        public static void WriteLine(ConsoleColor color, string output)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(output);
            System.Console.ResetColor();
        }

        public static void WriteLine(string format, params object[] values)
        {
            System.Console.WriteLine(format, values);
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] values)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(format, values);
            System.Console.ResetColor();
        }

        public static void WriteObject(ConsoleColor color, object obj)
        {
            var serializedObject = JsonConvert.SerializeObject(obj, Formatting.Indented);
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(serializedObject);
            System.Console.ResetColor();
        }
    }
}