namespace EnterpriseIntegrationPlatform.AI.Ollama;

/// <summary>
/// Configuration options for the Ollama LLM service.
/// Bound from the <c>Ollama</c> configuration section.
/// </summary>
public sealed class OllamaSettings
{
    /// <summary>
    /// The Ollama model name to use for generation and analysis.
    /// Defaults to <c>llama3.2</c> for local development;
    /// CI may override to a smaller model (e.g. <c>qwen2.5:0.5b</c>).
    /// </summary>
    public string Model { get; set; } = "llama3.2";
}
