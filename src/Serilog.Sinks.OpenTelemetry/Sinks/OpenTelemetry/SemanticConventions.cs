namespace Serilog.Sinks.OpenTelemetry;

/// <summary>
/// Well-known attribute names.
/// </summary>
/// <remarks>See also https://opentelemetry.io/docs/reference/specification/logs/semantic_conventions/.</remarks>
static class SemanticConventions
{
    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionType = "exception.type";

    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionMessage = "exception.message";
    
    /// <summary>
    /// OpenTelemetry standard exception type attribute.
    /// </summary>
    public const string AttributeExceptionStacktrace = "exception.stacktrace";

    /// <summary>
    /// A https://messagetemplates.org template, as text. For example, the string <c>Hello {Name}!</c>.
    /// </summary>
    public  const string AttributeMessageTemplateText = "message_template.text";

    /// <summary>
    /// A https://messagetemplates.org template, hashed using MD5 and encoded as a 128-bit hexadecimal value.
    /// </summary>
    public const string AttributeMessageTemplateMD5Hash = "message_template.hash.md5";
}
