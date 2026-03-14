namespace EnterpriseIntegrationPlatform.AI.Ollama;

/// <summary>
/// Interface for AI-assisted operations using Ollama.
/// Provides methods for natural-language analysis of message state,
/// integration health, and trace diagnostics.
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// Sends a prompt to the Ollama model and returns the generated response.
    /// </summary>
    /// <param name="prompt">The natural-language prompt to send.</param>
    /// <param name="model">The Ollama model name (default: "llama3.2").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated text response.</returns>
    Task<string> GenerateAsync(string prompt, string model = "llama3.2", CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyses structured context (e.g. a JSON trace or message state) and
    /// returns an AI-generated diagnostic summary.
    /// </summary>
    /// <param name="systemPrompt">System-level instructions for the model.</param>
    /// <param name="context">The structured data to analyse.</param>
    /// <param name="model">The Ollama model name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An AI-generated diagnostic summary.</returns>
    Task<string> AnalyseAsync(string systemPrompt, string context, string model = "llama3.2", CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when the Ollama service is reachable and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
