namespace Serilog.Sinks.OpenTelemetry.Exporters.ExportResults
{
    internal static class ExportResultAsTaskExtensions
    {
        public static async Task Match(this Task<ExportResult> result,
            Action<ExportResult> onSuccess,
            Action<ExportResult> onFailure)
        {
            var actualizedResult = await result;

            actualizedResult.Match(onSuccess, onFailure);
        }

        public static void Match(this ExportResult result,
            Action<ExportResult> onSuccess,
            Action<ExportResult> onFailure)
        {
            if (result.IsSuccess)
            {
                onSuccess(result);
            }
            else
            {
                onFailure(result);
            }
        }
    }
}
