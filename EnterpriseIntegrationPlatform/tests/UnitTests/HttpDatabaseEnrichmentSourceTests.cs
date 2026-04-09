using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Nodes;
using EnterpriseIntegrationPlatform.Processing.Transform;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

// ───── HttpEnrichmentSourceTests ─────

[TestFixture]
public sealed class HttpEnrichmentSourceTests
{
    private sealed class MockHandler : DelegatingHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(ResponseFactory(request));
    }

    private MockHandler _handler = null!;
    private IHttpClientFactory _httpClientFactory = null!;
    private ContentEnricherOptions _options = null!;
    private ILogger<HttpEnrichmentSource> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new MockHandler();
        var client = new HttpClient(_handler);
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient("ContentEnricher").Returns(client);

        _options = new ContentEnricherOptions
        {
            EndpointUrlTemplate = "https://api.example.com/customers/{key}",
            LookupKeyPath = "customerId",
            MergeTargetPath = "customer"
        };

        _logger = NullLogger<HttpEnrichmentSource>.Instance;
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
    }

    [Test]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new HttpEnrichmentSource(null!, _options, _logger),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new HttpEnrichmentSource(_httpClientFactory, null!, _logger),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new HttpEnrichmentSource(_httpClientFactory, _options, null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public async Task FetchAsync_SuccessfulResponse_ReturnsJsonNode()
    {
        _handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"test"}""")
        };

        var sut = new HttpEnrichmentSource(_httpClientFactory, _options, _logger);
        var result = await sut.FetchAsync("42");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["name"]!.GetValue<string>(), Is.EqualTo("test"));
    }

    [Test]
    public async Task FetchAsync_ReplacesKeyPlaceholderInUrl()
    {
        Uri? capturedUri = null;
        _handler.ResponseFactory = req =>
        {
            capturedUri = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        };

        var sut = new HttpEnrichmentSource(_httpClientFactory, _options, _logger);
        await sut.FetchAsync("abc-123");

        Assert.That(capturedUri, Is.Not.Null);
        Assert.That(capturedUri!.ToString(), Does.Contain("abc-123"));
        Assert.That(capturedUri.ToString(), Does.Not.Contain("{key}"));
    }

    [Test]
    public void FetchAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        _handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var sut = new HttpEnrichmentSource(_httpClientFactory, _options, _logger);

        Assert.That(
            async () => await sut.FetchAsync("fail"),
            Throws.TypeOf<HttpRequestException>());
    }
}

// ───── DatabaseEnrichmentSourceTests ─────

[TestFixture]
public sealed class DatabaseEnrichmentSourceTests
{
    private const string Sql = "SELECT name, tier FROM customers WHERE id = @key";
    private const string ParamName = "@key";

    private ILogger<DatabaseEnrichmentSource> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = NullLogger<DatabaseEnrichmentSource>.Instance;
    }

    [Test]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new DatabaseEnrichmentSource(null!, Sql, ParamName, _logger),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_EmptySql_ThrowsArgumentException()
    {
        Func<DbConnection> factory = () => Substitute.For<DbConnection>();
        Assert.That(
            () => new DatabaseEnrichmentSource(factory, "", ParamName, _logger),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_EmptyParameterName_ThrowsArgumentException()
    {
        Func<DbConnection> factory = () => Substitute.For<DbConnection>();
        Assert.That(
            () => new DatabaseEnrichmentSource(factory, Sql, "", _logger),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Func<DbConnection> factory = () => Substitute.For<DbConnection>();
        Assert.That(
            () => new DatabaseEnrichmentSource(factory, Sql, ParamName, null!),
            Throws.ArgumentNullException);
    }

    [Test]
    public async Task FetchAsync_NoRows_ReturnsNull()
    {
        var fakeReader = new FakeDbDataReader(columns: [], rows: []);
        var fakeCommand = new FakeDbCommand(fakeReader);
        var fakeConnection = new FakeDbConnection(fakeCommand);

        var sut = new DatabaseEnrichmentSource(() => fakeConnection, Sql, ParamName, _logger);
        var result = await sut.FetchAsync("missing-key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FetchAsync_WithRow_ReturnsJsonObject()
    {
        var columns = new[] { "name", "tier" };
        var rows = new[] { new object[] { "Alice", "gold" } };
        var fakeReader = new FakeDbDataReader(columns, rows);
        var fakeCommand = new FakeDbCommand(fakeReader);
        var fakeConnection = new FakeDbConnection(fakeCommand);

        var sut = new DatabaseEnrichmentSource(() => fakeConnection, Sql, ParamName, _logger);
        var result = await sut.FetchAsync("C-42");

        Assert.That(result, Is.Not.Null);
        var obj = result as JsonObject;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj!["name"]!.GetValue<string>(), Is.EqualTo("Alice"));
        Assert.That(obj["tier"]!.GetValue<string>(), Is.EqualTo("gold"));
    }

    // ───── Lightweight ADO.NET test doubles ─────

    private sealed class FakeDbConnection(DbCommand command) : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => string.Empty;
        public override string DataSource => string.Empty;
        public override string ServerVersion => string.Empty;
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => null!;
        protected override DbCommand CreateDbCommand() => command;
    }

    private sealed class FakeDbCommand(DbDataReader reader) : DbCommand
    {
        private readonly FakeDbParameterCollection _parameters = new();
        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => reader;
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];
        public override int Count => _items.Count;
        public override object SyncRoot => _items;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => _items.Exists(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) { }
        public override IEnumerator<DbParameter> GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _items[idx] = value;
        }
    }

    private sealed class FakeDbDataReader(string[] columns, object[][] rows) : DbDataReader
    {
        private int _currentRow = -1;
        public override int FieldCount => columns.Length;
        public override int RecordsAffected => 0;
        public override bool HasRows => rows.Length > 0;
        public override bool IsClosed => false;
        public override int Depth => 0;
        public override object this[int ordinal] => rows[_currentRow][ordinal];
        public override object this[string name] => this[GetOrdinal(name)];
        public override bool Read() => ++_currentRow < rows.Length;
        public override string GetName(int ordinal) => columns[ordinal];
        public override int GetOrdinal(string name) => Array.IndexOf(columns, name);
        public override object GetValue(int ordinal) => rows[_currentRow][ordinal];
        public override bool IsDBNull(int ordinal) => rows[_currentRow][ordinal] is null || rows[_currentRow][ordinal] == DBNull.Value;
        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => "string";
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
        public override Type GetFieldType(int ordinal) => GetValue(ordinal).GetType();
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);
        public override int GetValues(object[] values) { Array.Copy(rows[_currentRow], values, Math.Min(values.Length, FieldCount)); return Math.Min(values.Length, FieldCount); }
        public override bool NextResult() => false;
        public override IEnumerator<DbDataReader> GetEnumerator() => Enumerable.Empty<DbDataReader>().GetEnumerator();
    }
}
