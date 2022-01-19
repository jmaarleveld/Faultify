using Faultify.Report.Reporters;

namespace Faultify.Report
{
    public class ReporterFactory
    {
        /// <summary>
        ///     Create and return an IReporter
        /// </summary>
        /// <param name="type">Reporter file type</param>
        /// <returns>IReporter</returns>
        public static IReporter CreateReporter(ReporterType type)
        {
            return type switch
            {
                ReporterType.Pdf => new PdfReporter(),
                ReporterType.Html => new HtmlReporter(),
                ReporterType.Json => new JsonReporter(),
                _ => new JsonReporter(),
            };
        }
    }
}