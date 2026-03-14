namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Base class for all platform activities.
/// </summary>
public abstract class BaseActivity
{
    /// <summary>Activity name for identification and logging.</summary>
    public abstract string Name { get; }
}
