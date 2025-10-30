namespace HelloFoundry.Web.Configuration;

public class AiModelOptions
{
    public const string SectionName = "AiModel";
    
    public string ModelAlias { get; set; } = "qwen2.5-0.5b";
    public int InitializationTimeoutSeconds { get; set; } = 60;
    public int StreamingDelayMs { get; set; } = 10;
    public bool EnableStreamingDelay { get; set; } = true;
}

public class ApiOptions
{
    public const string SectionName = "Api";
    
    public int MaxMessageLength { get; set; } = 4000;
    public string[] AllowedOrigins { get; set; } = ["*"];
    public bool EnableDetailedErrors { get; set; } = false;
}