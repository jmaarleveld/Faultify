using System;
using NLog;

namespace Faultify.Report.Reporters
{
    public class ReporterFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static IReporter CreateReporter(string type)
        {
            try
            {
                return type.ToUpper() switch
                {
                    "PDF" => new PdfReporter(),
                    "HTML" => new HtmlReporter(),
                    "JSON" => new JsonReporter(),
                    _ => throw new ArgumentOutOfRangeException(type, $"The argument \"{type}\" is not a valid file output type." +
                                                                     "Defaulting to JSON."),
                };
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error(ex, ex.Message);
                return new JsonReporter();
            }
        }
    }
}