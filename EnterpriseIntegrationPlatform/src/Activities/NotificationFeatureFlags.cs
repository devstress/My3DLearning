namespace EnterpriseIntegrationPlatform.Activities;

/// <summary>
/// Well-known feature flag names for the notification framework.
/// </summary>
public static class NotificationFeatureFlags
{
    /// <summary>
    /// Master toggle for the Ack/Nack notification framework.
    /// <para>
    /// When this flag is disabled, no Ack or Nack notifications are published
    /// even if the integration has <c>NotificationsEnabled = true</c>.
    /// When re-enabled, notifications resume for all integrations that have
    /// <c>NotificationsEnabled = true</c>.
    /// </para>
    /// <para>
    /// This supports the operational scenario where an operator needs to
    /// temporarily silence all notifications (e.g. during maintenance or
    /// incident response) without changing individual integration configurations.
    /// </para>
    /// </summary>
    public const string NotificationsEnabled = "Notifications.Enabled";
}
