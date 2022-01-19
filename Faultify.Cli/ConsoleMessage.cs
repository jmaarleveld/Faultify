using System;

namespace Faultify.Cli
{
    internal static class ConsoleMessage
    {
        private static readonly string _logo = @"    
             ______            ____  _ ____     
            / ____/___ ___  __/ / /_(_) __/_  __
           / /_  / __ `/ / / / / __/ / /_/ / / / 
          / __/ / /_/ / /_/ / / /_/ / __/ /_/ / 
         /_/    \__,_/\__,_/_/\__/_/_/  \___,/ 
                                       /____/  
        ";

        public static void PrintLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(_logo);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void PrintSettings(Settings settings)
        {
            string settingsString =
                "\n"
                + $"| Test Project Path: {settings.TestProjectPath}\n"
                + $"| Mutation Level: {settings.MutationLevel}\n"
                + $"| Test host: {nameof(settings.TestHost)}\n"
                + $"| Report Path: {settings.ReportPath}\n"
                + $"| Report Type: {settings.ReportType}\n"
                + "\n";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(settingsString);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
