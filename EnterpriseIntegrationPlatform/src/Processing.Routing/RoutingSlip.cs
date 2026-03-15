using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Processing.Routing;

/// <summary>
/// A single step in a routing slip defining destination and processing metadata.
/// </summary>
/// <param name="Destination">Endpoint/service name for this step.</param>
/// <param name="Metadata">Optional key-value data for the step processor.</param>
public record RoutingSlipStep(
    string Destination,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// Routing Slip — attaches a sequence of processing steps to a message.
/// Each processor handles its step and forwards to the next.
/// Equivalent to BizTalk itinerary-based routing in ESB Toolkit.
/// </summary>
public sealed class RoutingSlip<T>
{
    private readonly List<RoutingSlipStep> _steps;
    private int _currentIndex;

    public RoutingSlip(IEnumerable<RoutingSlipStep> steps)
    {
        _steps = steps.ToList();
        _currentIndex = 0;
    }

    /// <summary>All steps in this routing slip.</summary>
    public IReadOnlyList<RoutingSlipStep> Steps => _steps;

    /// <summary>Index of the current (next-to-process) step.</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>The current step, or null if all steps are complete.</summary>
    public RoutingSlipStep? CurrentStep =>
        _currentIndex < _steps.Count ? _steps[_currentIndex] : null;

    /// <summary>True when all steps have been processed.</summary>
    public bool IsComplete => _currentIndex >= _steps.Count;

    /// <summary>Advances to the next step.</summary>
    public RoutingSlipStep? Advance()
    {
        if (_currentIndex < _steps.Count)
            _currentIndex++;

        return CurrentStep;
    }
}
