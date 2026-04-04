using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

using EnterpriseIntegrationPlatform.AI.RagKnowledge;

namespace EnterpriseIntegrationPlatform.Tests.Unit;

[TestFixture]
public class RagDocumentParserTests
{
    private RagDocumentParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new RagDocumentParser(Substitute.For<ILogger<RagDocumentParser>>());
    }

    [Test]
    public void Parse_ValidXml_ReturnsDocuments()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ragKnowledgeBase xmlns="urn:eip:rag:v1" category="Test Category">
              <document id="test-doc" title="Test Document" pattern="Test Pattern">
                <summary>This is a test summary.</summary>
                <implementation>Test implementation details.</implementation>
                <components>TestComponent</components>
                <tags>test unit sample</tags>
              </document>
            </ragKnowledgeBase>
            """;

        // Act
        var result = _parser.Parse(xml);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("test-doc"));
        Assert.That(result[0].Title, Is.EqualTo("Test Document"));
        Assert.That(result[0].Pattern, Is.EqualTo("Test Pattern"));
        Assert.That(result[0].Category, Is.EqualTo("Test Category"));
        Assert.That(result[0].Summary, Is.EqualTo("This is a test summary."));
        Assert.That(result[0].Implementation, Is.EqualTo("Test implementation details."));
        Assert.That(result[0].Components, Is.EqualTo("TestComponent"));
        Assert.That(result[0].Tags, Is.EqualTo(new[] { "test", "unit", "sample" }));
    }

    [Test]
    public void Parse_MultipleDocuments_ReturnsAll()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ragKnowledgeBase xmlns="urn:eip:rag:v1" category="Routing">
              <document id="doc-1" title="Router A" pattern="Pattern A">
                <summary>Summary A</summary>
                <implementation>Impl A</implementation>
                <components>CompA</components>
                <tags>routing alpha</tags>
              </document>
              <document id="doc-2" title="Router B" pattern="Pattern B">
                <summary>Summary B</summary>
                <implementation>Impl B</implementation>
                <components>CompB</components>
                <tags>routing beta</tags>
              </document>
            </ragKnowledgeBase>
            """;

        // Act
        var result = _parser.Parse(xml);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("doc-1"));
        Assert.That(result[1].Id, Is.EqualTo("doc-2"));
    }

    [Test]
    public void Parse_InvalidXml_ReturnsEmpty()
    {
        // Act
        var result = _parser.Parse("<not valid xml!!!");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
    }

    [Test]
    public void Parse_MissingOptionalFields_DefaultsToEmpty()
    {
        // Arrange — document with no tags or implementation element
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ragKnowledgeBase xmlns="urn:eip:rag:v1" category="Minimal">
              <document id="min-doc" title="Minimal" pattern="Min">
                <summary>Just a summary</summary>
              </document>
            </ragKnowledgeBase>
            """;

        // Act
        var result = _parser.Parse(xml);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Implementation, Is.EqualTo(string.Empty));
        Assert.That(result[0].Components, Is.EqualTo(string.Empty));
        Assert.That(result[0].Tags, Is.Empty);
    }
}

[TestFixture]
public class RagKnowledgeIndexTests
{
    private RagKnowledgeIndex _index = null!;

    [SetUp]
    public void SetUp()
    {
        _index = new RagKnowledgeIndex(Substitute.For<ILogger<RagKnowledgeIndex>>());
    }

    [Test]
    public void AddDocuments_IncreasesDocumentCount()
    {
        // Arrange
        var docs = new[]
        {
            new RagDocument("d1", "Router", "Content-Based Router", "Routing", "Routes messages", "Processing.Routing", "Routing", ["routing", "content"]),
            new RagDocument("d2", "Filter", "Message Filter", "Routing", "Filters messages", "Processing.Routing", "Routing", ["filter", "routing"]),
        };

        // Act
        _index.AddDocuments(docs);

        // Assert
        Assert.That(_index.DocumentCount, Is.EqualTo(2));
    }

    [Test]
    public void GetById_ReturnsCorrectDocument()
    {
        // Arrange
        var doc = new RagDocument("cbr", "Content-Based Router", "Content-Based Router", "Routing", "Routes based on content", "Impl", "Comp", ["routing"]);
        _index.AddDocuments([doc]);

        // Act
        var result = _index.GetById("cbr");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Content-Based Router"));
    }

    [Test]
    public void GetById_NonExistent_ReturnsNull()
    {
        Assert.That(_index.GetById("no-such-id"), Is.Null);
    }

    [Test]
    public void GetByTag_ReturnsMatchingDocuments()
    {
        // Arrange
        var docs = new[]
        {
            new RagDocument("d1", "Router", "Router", "Routing", "S", "I", "C", ["routing", "content"]),
            new RagDocument("d2", "Filter", "Filter", "Routing", "S", "I", "C", ["filter", "routing"]),
            new RagDocument("d3", "Enricher", "Enricher", "Transform", "S", "I", "C", ["transform"]),
        };
        _index.AddDocuments(docs);

        // Act
        var result = _index.GetByTag("routing");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetByCategory_ReturnsDocumentsInCategory()
    {
        // Arrange
        var docs = new[]
        {
            new RagDocument("d1", "T1", "P1", "Routing", "S", "I", "C", []),
            new RagDocument("d2", "T2", "P2", "Transform", "S", "I", "C", []),
            new RagDocument("d3", "T3", "P3", "Routing", "S", "I", "C", []),
        };
        _index.AddDocuments(docs);

        // Act
        var result = _index.GetByCategory("Routing");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetAllTags_ReturnsSortedUniqueKeys()
    {
        // Arrange
        var docs = new[]
        {
            new RagDocument("d1", "T1", "P1", "C", "S", "I", "C", ["beta", "alpha"]),
            new RagDocument("d2", "T2", "P2", "C", "S", "I", "C", ["gamma", "alpha"]),
        };
        _index.AddDocuments(docs);

        // Act
        var tags = _index.GetAllTags();

        // Assert — should include tags plus auto-indexed title/category/pattern words
        Assert.That(tags, Does.Contain("alpha"));
        Assert.That(tags, Does.Contain("beta"));
        Assert.That(tags, Does.Contain("gamma"));
    }
}

[TestFixture]
public class RagQueryMatcherTests
{
    private RagKnowledgeIndex _index = null!;
    private RagQueryMatcher _matcher = null!;

    [SetUp]
    public void SetUp()
    {
        _index = new RagKnowledgeIndex(Substitute.For<ILogger<RagKnowledgeIndex>>());
        _matcher = new RagQueryMatcher(_index, Substitute.For<ILogger<RagQueryMatcher>>());

        var docs = new[]
        {
            new RagDocument("cbr", "Content-Based Router", "Content-Based Router", "Message Routing",
                "Routes messages based on content", "Processing.Routing.ContentBasedRouter", "Processing.Routing",
                ["content", "routing", "router", "rules"]),
            new RagDocument("filter", "Message Filter", "Message Filter", "Message Routing",
                "Eliminates undesired messages", "Processing.Routing.MessageFilter", "Processing.Routing",
                ["filter", "discard", "predicate"]),
            new RagDocument("enricher", "Content Enricher", "Content Enricher", "Message Transformation",
                "Augments messages with external data", "Processing.Transform.ContentEnricher", "Processing.Transform",
                ["enricher", "augment", "transform"]),
            new RagDocument("dlq", "Dead Letter Channel", "Dead Letter Channel", "Messaging Channels",
                "Failed messages routed to DLQ", "Processing.DeadLetter", "Processing.DeadLetter",
                ["dead", "letter", "dlq", "retry", "failure"]),
        };
        _index.AddDocuments(docs);
    }

    [Test]
    public void Search_MatchingKeywords_ReturnsRankedResults()
    {
        // Act
        var results = _matcher.Search("content-based routing");

        // Assert
        Assert.That(results, Is.Not.Empty);
        Assert.That(results[0].Document.Id, Is.EqualTo("cbr"));
        Assert.That(results[0].Score, Is.GreaterThan(0));
        Assert.That(results[0].Score, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var results = _matcher.Search("kubernetes deployment helm");

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_EmptyQuery_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _matcher.Search(""));
    }

    [Test]
    public void Search_RespectsMaxResults()
    {
        var results = _matcher.Search("routing filter content message", maxResults: 2);

        Assert.That(results.Count, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void Search_ScoresAreNormalized()
    {
        var results = _matcher.Search("content routing filter");

        // Top result should have score 1.0 (normalized max)
        Assert.That(results, Is.Not.Empty);
        Assert.That(results[0].Score, Is.EqualTo(1.0));
        // Other results should be < 1.0
        if (results.Count > 1)
        {
            Assert.That(results[1].Score, Is.LessThanOrEqualTo(1.0));
        }
    }

    [Test]
    public void ExtractKeywords_RemovesStopWords()
    {
        var keywords = RagQueryMatcher.ExtractKeywords("How does the content-based router work in this system?");

        Assert.That(keywords, Does.Contain("content"));
        Assert.That(keywords, Does.Contain("based"));
        Assert.That(keywords, Does.Contain("router"));
        Assert.That(keywords, Does.Contain("work"));
        Assert.That(keywords, Does.Contain("system"));
        // Stop words removed
        Assert.That(keywords, Does.Not.Contain("how"));
        Assert.That(keywords, Does.Not.Contain("does"));
        Assert.That(keywords, Does.Not.Contain("the"));
        Assert.That(keywords, Does.Not.Contain("in"));
        Assert.That(keywords, Does.Not.Contain("this"));
    }

    [Test]
    public void Search_DlqQuery_FindsDeadLetterChannel()
    {
        var results = _matcher.Search("dead letter queue failure retry");

        Assert.That(results, Is.Not.Empty);
        Assert.That(results[0].Document.Id, Is.EqualTo("dlq"));
    }

    [Test]
    public void ExtractKeywords_OnlyStopWords_ReturnsEmpty()
    {
        var keywords = RagQueryMatcher.ExtractKeywords("the is a an in for to of on by");

        Assert.That(keywords, Is.Empty);
    }
}

[TestFixture]
public class RagDocumentRecordTests
{
    [Test]
    public void RagDocument_ShouldHoldValues()
    {
        var doc = new RagDocument("id-1", "Title", "Pattern", "Category", "Summary", "Impl", "Comp", ["tag1", "tag2"]);

        Assert.That(doc.Id, Is.EqualTo("id-1"));
        Assert.That(doc.Title, Is.EqualTo("Title"));
        Assert.That(doc.Pattern, Is.EqualTo("Pattern"));
        Assert.That(doc.Category, Is.EqualTo("Category"));
        Assert.That(doc.Summary, Is.EqualTo("Summary"));
        Assert.That(doc.Implementation, Is.EqualTo("Impl"));
        Assert.That(doc.Components, Is.EqualTo("Comp"));
        Assert.That(doc.Tags, Has.Count.EqualTo(2));
    }

    [Test]
    public void RagQueryResult_ShouldHoldValues()
    {
        var doc = new RagDocument("id", "T", "P", "C", "S", "I", "Co", []);
        var result = new RagQueryResult(doc, 0.85);

        Assert.That(result.Document.Id, Is.EqualTo("id"));
        Assert.That(result.Score, Is.EqualTo(0.85));
    }
}
