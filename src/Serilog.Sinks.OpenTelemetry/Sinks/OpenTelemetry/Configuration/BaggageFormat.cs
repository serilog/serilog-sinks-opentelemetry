namespace Serilog.Sinks.OpenTelemetry.Configuration;

static class BaggageFormat
{
    /// <summary>
    /// Decode W3C Baggage-formatted key-value pairs as specified for handling of the `OTEL_EXPORTER_OTLP_HEADERS` and
    /// `OTEL_RESOURCE_ATTRIBUTES` environment variables.
    /// </summary>
    /// <returns>The property names and values encoded in the supplied <paramref name="baggageString"/>.</returns>
    public static IEnumerable<(string, string)> DecodeBaggageString(string baggageString, string environmentVariableName)
    {
        // See: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable
        // See: https://www.w3.org/TR/baggage/#header-content
        
        if (string.IsNullOrWhiteSpace(baggageString)) yield break;
        
        foreach (var listMember in baggageString.Split(','))
        {
            // The baggage spec allows list members to carry additional key-value pair metadata after the initial
            // key and value and a trailing semicolon, but this is disallowed by the OTel spec. We're pretty loose with
            // validation, here, but could tighten up handling of invalid values in the future.

            var eq = listMember.IndexOf('=');
            if (eq == -1) RejectInvalidListMember(listMember, environmentVariableName, nameof(baggageString));

            var key = listMember.Substring(0, eq).Trim();
            if (string.IsNullOrEmpty(key)) RejectInvalidListMember(listMember, environmentVariableName, nameof(baggageString));

            var escapedValue = eq == listMember.Length - 1 ? "" : listMember.Substring(eq + 1).Trim();
            var value = Uri.UnescapeDataString(escapedValue);

            yield return (key, value);
        }
    }

    static void RejectInvalidListMember(string listMember, string environmentVariableName, string paramName)
    {
        throw new ArgumentException($"Invalid item format `{listMember}` in {environmentVariableName} environment variable.", paramName);
    }
}