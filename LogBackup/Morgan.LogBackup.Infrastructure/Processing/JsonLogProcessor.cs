using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Core.Contracts;

namespace Morgan.LogBackup.Infrastructure.Processing
{
    /// <summary>
    /// Processes raw JSON log lines by applying masking rules, removing unwanted
    /// fields, skipping unwanted records, and normalizing log levels.
    /// </summary>
    /// <remarks>
    /// - Reads masking and processing behavior from configuration  
    /// - Supports full-field masking (replaces with ********)  
    /// - Supports partial masking based on visible character rules  
    /// - Skips entire log entries when configured fields exist  
    /// - Removes configured fields entirely  
    /// - Normalizes log level values to a consistent standard  
    /// - Invalid JSON lines are skipped safely without interrupting processing  
    /// </remarks>
    public class JsonLogProcessor : ILogProcessor
    {
        private readonly HashSet<string> _fullMask;
        private readonly Dictionary<string, MaskRule> _partialMask;
        private readonly HashSet<string> _skipIf;
        private readonly HashSet<string> _skipFields;
        private readonly string? _logLevelField;
        private readonly Dictionary<string, string> _logLevelMap;
        private readonly ILogger<JsonLogProcessor> _logger;

        public JsonLogProcessor(IConfiguration config, ILogger<JsonLogProcessor> logger)
        {
            _logger = logger;
            _fullMask = config
            .GetSection("Processing:MaskingRules:Full")
            .GetChildren()
            .Select(x => x.Value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _partialMask = config
                .GetSection("Processing:MaskingRules:Partial")
                .GetChildren()
                .ToDictionary(
                    x => x.Key,
                    x => x.Get<MaskRule>()!,
                    StringComparer.OrdinalIgnoreCase
                );
            _skipIf = config.GetSection("Processing:SkipIfContains").Get<string[]>()!.ToHashSet();
            _skipFields = config.GetSection("Processing:SkipFields").Get<string[]>()!.ToHashSet();

            var levelSection = config.GetSection("Processing:LogLevelNormalization");

            _logLevelField = levelSection["FieldName"];

            _logLevelMap = levelSection
                .GetSection("Mappings")
                .GetChildren()
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Processes a single raw JSON log line.
        /// Applies masking, skip rules, field removal, and log level normalization.
        /// </summary>
        /// <param name="rawLine">The raw JSON log line.</param>
        /// <returns>
        /// A sanitized JSON string if processing succeeds,
        /// or null if the entry should be skipped or is invalid.
        /// </returns>
        public string? Process(string rawLine)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawLine);
                var root = doc.RootElement;

                foreach (var s in _skipIf)
                    if (root.TryGetProperty(s, out _))
                        return null;

                var output = new Dictionary<string, object?>();
                //This dictionary object contains non transformed values
               
                foreach (var prop in root.EnumerateObject())
                {
                    if (_skipFields.Contains(prop.Name)) continue;

                    if (_fullMask.Contains(prop.Name))
                    {
                        output[prop.Name] = "********";
                        continue;
                    }

                    if (_partialMask.TryGetValue(prop.Name, out var rule))
                    {
                        output[prop.Name] = ApplyPartialMask(prop.Value.ToString(), rule);
                        continue;
                    }

                    if (_logLevelField != null && prop.Name.Equals(_logLevelField, StringComparison.OrdinalIgnoreCase))
                    {
                        var rawValue = prop.Value.ToString();

                        if (_logLevelMap.TryGetValue(rawValue, out var normalized))
                            output[prop.Name] = normalized;
                        else
                            output[prop.Name] = rawValue;

                        continue;
                    }

                    output[prop.Name] = prop.Value.Deserialize<object>();
                }

                //This output dictionay object has transformed values
                return JsonSerializer.Serialize(output); //it convert dictinary back to json
            }
            catch (JsonException)
            {
                // Invalid JSON 
                _logger.LogWarning($"Skipping invalid JSON log line. {rawLine}");
                return null;
            }
            catch (Exception ex)
            {
                // Serious unexpected problem — log error
                _logger.LogError(ex, $"Unexpected error while processing log line. {rawLine}");
                return null;
            }
        }

        private static string ApplyPartialMask(string value, MaskRule rule)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var start = Math.Min(rule.VisibleStart, value.Length);
            var end = Math.Min(rule.VisibleEnd, value.Length - start);

            var maskedLength = value.Length - start - end;
            if (maskedLength <= 0)
                return value;

            return value.Substring(0, start)
                   + new string('*', maskedLength)
                   + value.Substring(value.Length - end);
        }
    }

}
