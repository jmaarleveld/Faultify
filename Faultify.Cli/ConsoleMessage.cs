using Faultify.Pipeline;
using Microsoft.Extensions.Logging;

namespace Faultify.Cli
{
    internal static class ConsoleMessage
    {
        private static readonly string _logo =
@"    ______            ____  _ ____     
   / ____/___ ___  __/ / /_(_) __/_  __
  / /_  / __ `/ / / / / __/ / /_/ / / / 
 / __/ / /_/ / /_/ / / /_/ / __/ /_/ / 
/_/    \__,_/\__,_/_/\__/_/_/  \___,/ 
                              /____/  
";

        public static void PrintLogo()
        {
            Program.ConsoleLogger.LogInformation(_logo);
        }

        public static void PrintSettings(Settings settings)
        {
            string settingsString =
                $"> Test Project Path: {settings.TestProjectPath}\n"
                + $"      | Mutation Level: {settings.MutationLevel}\n"
                + $"      | Test host: {settings.TestHost}\n"
                + $"      | Report Path: {settings.ReportPath}\n"
                + $"      | Report Type: {settings.ReportType}\n";
            
            Program.ConsoleLogger.LogInformation(settingsString);
        }
    }
}
