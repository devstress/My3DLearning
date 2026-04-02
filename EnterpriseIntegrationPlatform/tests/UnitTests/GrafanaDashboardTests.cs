using System.Text.Json;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class GrafanaDashboardTests
{
    private static readonly string _dashboardDirectory = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..",
            "deploy", "grafana", "dashboards"));

    private static readonly string _provisioningDirectory = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..",
            "deploy", "grafana", "provisioning"));

    private static readonly string[] _expectedDashboardFiles =
    [
        "platform-health.json",
        "message-throughput.json",
        "connector-status.json",
        "temporal-workflows.json",
        "dlq-overview.json"
    ];

    private static readonly string[] _expectedUids =
    [
        "eip-platform-health",
        "eip-message-throughput",
        "eip-connector-status",
        "eip-temporal-workflows",
        "eip-dlq-overview"
    ];

    [Test]
    public void AllDashboardFiles_Exist()
    {
        foreach (var file in _expectedDashboardFiles)
        {
            var path = Path.Combine(_dashboardDirectory, file);
            Assert.That(File.Exists(path), Is.True, $"Dashboard file not found: {file}");
        }
    }

    [TestCaseSource(nameof(_expectedDashboardFiles))]
    public void DashboardFile_IsValidJson(string fileName)
    {
        var path = Path.Combine(_dashboardDirectory, fileName);
        var json = File.ReadAllText(path);

        Assert.DoesNotThrow(() => JsonDocument.Parse(json),
            $"Dashboard file is not valid JSON: {fileName}");
    }

    [TestCaseSource(nameof(_expectedDashboardFiles))]
    public void DashboardFile_ContainsRequiredStructure(string fileName)
    {
        var path = Path.Combine(_dashboardDirectory, fileName);
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        Assert.That(root.TryGetProperty("title", out _), Is.True,
            $"Dashboard {fileName} is missing 'title'");
        Assert.That(root.TryGetProperty("uid", out _), Is.True,
            $"Dashboard {fileName} is missing 'uid'");
        Assert.That(root.TryGetProperty("panels", out var panels), Is.True,
            $"Dashboard {fileName} is missing 'panels'");
        Assert.That(panels.GetArrayLength(), Is.GreaterThan(0),
            $"Dashboard {fileName} has no panels");
        Assert.That(root.TryGetProperty("templating", out _), Is.True,
            $"Dashboard {fileName} is missing 'templating'");
        Assert.That(root.TryGetProperty("schemaVersion", out var schemaVersion), Is.True,
            $"Dashboard {fileName} is missing 'schemaVersion'");
        Assert.That(schemaVersion.GetInt32(), Is.GreaterThanOrEqualTo(39),
            $"Dashboard {fileName} schemaVersion should be >= 39");
    }

    [Test]
    public void AllDashboards_HaveUniqueUids()
    {
        var uids = new HashSet<string>();

        foreach (var file in _expectedDashboardFiles)
        {
            var path = Path.Combine(_dashboardDirectory, file);
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            var uid = doc.RootElement.GetProperty("uid").GetString()!;

            Assert.That(uids.Add(uid), Is.True,
                $"Duplicate dashboard uid found: {uid} in {file}");
        }
    }

    [Test]
    public void AllDashboards_HaveExpectedUids()
    {
        var actualUids = new List<string>();

        foreach (var file in _expectedDashboardFiles)
        {
            var path = Path.Combine(_dashboardDirectory, file);
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            actualUids.Add(doc.RootElement.GetProperty("uid").GetString()!);
        }

        foreach (var expectedUid in _expectedUids)
        {
            Assert.That(actualUids, Does.Contain(expectedUid),
                $"Expected dashboard uid not found: {expectedUid}");
        }
    }

    [TestCaseSource(nameof(_expectedDashboardFiles))]
    public void DashboardFile_HasDatasourceVariable(string fileName)
    {
        var path = Path.Combine(_dashboardDirectory, fileName);
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var templating = doc.RootElement.GetProperty("templating");
        var list = templating.GetProperty("list");

        var hasDatasourceVar = false;
        foreach (var variable in list.EnumerateArray())
        {
            if (variable.TryGetProperty("type", out var type) &&
                type.GetString() == "datasource")
            {
                hasDatasourceVar = true;
                break;
            }
        }

        Assert.That(hasDatasourceVar, Is.True,
            $"Dashboard {fileName} has no datasource template variable");
    }

    [TestCaseSource(nameof(_expectedDashboardFiles))]
    public void DashboardFile_PanelsHaveIds(string fileName)
    {
        var path = Path.Combine(_dashboardDirectory, fileName);
        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var panels = doc.RootElement.GetProperty("panels");

        foreach (var panel in panels.EnumerateArray())
        {
            Assert.That(panel.TryGetProperty("id", out _), Is.True,
                $"Panel in {fileName} missing 'id'");
            Assert.That(panel.TryGetProperty("gridPos", out _), Is.True,
                $"Panel in {fileName} missing 'gridPos'");
        }
    }

    [Test]
    public void ProvisioningConfig_DatasourcePrometheus_Exists()
    {
        var path = Path.Combine(_provisioningDirectory, "datasources", "prometheus.yaml");
        Assert.That(File.Exists(path), Is.True, "Prometheus datasource config not found");

        var content = File.ReadAllText(path);
        Assert.That(content, Does.Contain("prometheus"),
            "Prometheus datasource config missing 'prometheus' type");
    }

    [Test]
    public void ProvisioningConfig_DatasourceLoki_Exists()
    {
        var path = Path.Combine(_provisioningDirectory, "datasources", "loki.yaml");
        Assert.That(File.Exists(path), Is.True, "Loki datasource config not found");

        var content = File.ReadAllText(path);
        Assert.That(content, Does.Contain("loki"),
            "Loki datasource config missing 'loki' type");
    }

    [Test]
    public void ProvisioningConfig_DashboardProvider_Exists()
    {
        var path = Path.Combine(_provisioningDirectory, "dashboards", "dashboards.yaml");
        Assert.That(File.Exists(path), Is.True, "Dashboard provisioning config not found");

        var content = File.ReadAllText(path);
        Assert.That(content, Does.Contain("/var/lib/grafana/dashboards"),
            "Dashboard provisioning config missing dashboard path");
    }

    [Test]
    public void ProvisioningConfig_AlertingRules_Exists()
    {
        var path = Path.Combine(_provisioningDirectory, "alerting", "alerts.yaml");
        Assert.That(File.Exists(path), Is.True, "Alerting rules config not found");

        var content = File.ReadAllText(path);
        Assert.That(content, Does.Contain("eip-high-error-rate"),
            "Missing high error rate alert rule");
        Assert.That(content, Does.Contain("eip-dlq-depth-threshold"),
            "Missing DLQ depth threshold alert rule");
        Assert.That(content, Does.Contain("eip-service-down"),
            "Missing service down alert rule");
        Assert.That(content, Does.Contain("eip-high-latency"),
            "Missing high latency alert rule");
        Assert.That(content, Does.Contain("eip-workflow-failures"),
            "Missing workflow failures alert rule");
    }

    [Test]
    public void AlertingRules_ReferenceValidDatasources()
    {
        var path = Path.Combine(_provisioningDirectory, "alerting", "alerts.yaml");
        var content = File.ReadAllText(path);

        // All data queries should reference prometheus or __expr__ datasource
        Assert.That(content, Does.Contain("datasourceUid: prometheus"),
            "Alert rules should reference the prometheus datasource");
    }

    [TestCaseSource(nameof(_expectedDashboardFiles))]
    public void DashboardFile_ReferencesPrometheusDataSource(string fileName)
    {
        var path = Path.Combine(_dashboardDirectory, fileName);
        var content = File.ReadAllText(path);

        Assert.That(content, Does.Contain("\"type\": \"prometheus\""),
            $"Dashboard {fileName} should reference Prometheus datasource type");
    }
}
