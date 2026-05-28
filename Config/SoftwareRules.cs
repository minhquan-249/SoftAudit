using System.Text.Json.Serialization;

public class SoftwareRules
{
    [JsonPropertyName("name_mapping")]
    public Dictionary<string, string> NameMapping { get; set; } = new();

    [JsonPropertyName("category_mapping")]
    public Dictionary<string, string> CategoryMapping { get; set; } = new();

    [JsonPropertyName("exclusion_rules")]
    public List<string> ExclusionRules { get; set; } = new();

    [JsonPropertyName("inclusion_rules")]
    public List<string> InclusionRules { get; set; } = new();
    [JsonPropertyName("license_rules")]
    public Dictionary<string, string> LicenseRules { get; set; } = new();
}