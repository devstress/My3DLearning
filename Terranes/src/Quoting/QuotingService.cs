using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terranes.Contracts.Abstractions;
using Terranes.Contracts.Enums;
using Terranes.Contracts.Models;

namespace Terranes.Quoting;

/// <summary>
/// In-memory implementation of <see cref="IQuotingService"/>.
/// Manages end-to-end quote requests and line items from partners.
/// </summary>
public sealed class QuotingService : IQuotingService
{
    private readonly ConcurrentDictionary<Guid, QuoteRequest> _requests = new();
    private readonly ConcurrentDictionary<Guid, List<QuoteLineItem>> _lineItems = new();
    private readonly ILogger<QuotingService> _logger;

    public QuotingService(ILogger<QuotingService> logger) => _logger = logger;

    public Task<QuoteRequest> RequestQuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SitePlacementId == Guid.Empty)
            throw new ArgumentException("Site placement ID is required.", nameof(request));

        if (request.RequestedByUserId == Guid.Empty)
            throw new ArgumentException("Requesting user ID is required.", nameof(request));

        var persisted = request with
        {
            Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            Status = QuoteStatus.Pending,
            RequestedAtUtc = DateTimeOffset.UtcNow
        };

        if (!_requests.TryAdd(persisted.Id, persisted))
            throw new InvalidOperationException($"Quote request with ID {persisted.Id} already exists.");

        _lineItems.TryAdd(persisted.Id, []);
        _logger.LogInformation("Created quote request {QuoteRequestId} for placement {PlacementId}", persisted.Id, persisted.SitePlacementId);
        return Task.FromResult(persisted);
    }

    public Task<QuoteRequest?> GetQuoteRequestAsync(Guid quoteRequestId, CancellationToken cancellationToken = default)
    {
        _requests.TryGetValue(quoteRequestId, out var request);
        return Task.FromResult(request);
    }

    public Task<IReadOnlyList<QuoteLineItem>> GetLineItemsAsync(Guid quoteRequestId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuoteLineItem> items = _lineItems.TryGetValue(quoteRequestId, out var list)
            ? list.AsReadOnly()
            : [];
        return Task.FromResult(items);
    }

    /// <summary>
    /// Adds a line item from a partner to an existing quote request and updates the request status.
    /// </summary>
    public Task<QuoteLineItem> AddLineItemAsync(QuoteLineItem lineItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lineItem);

        if (!_requests.ContainsKey(lineItem.QuoteRequestId))
            throw new InvalidOperationException($"Quote request {lineItem.QuoteRequestId} not found.");

        if (lineItem.AmountAud < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(lineItem));

        var persisted = lineItem with
        {
            Id = lineItem.Id == Guid.Empty ? Guid.NewGuid() : lineItem.Id,
            ProvidedAtUtc = DateTimeOffset.UtcNow
        };

        _lineItems.AddOrUpdate(
            lineItem.QuoteRequestId,
            _ => [persisted],
            (_, list) => { list.Add(persisted); return list; });

        // Update the request status to InProgress
        _requests.AddOrUpdate(
            lineItem.QuoteRequestId,
            _ => throw new InvalidOperationException("Quote request not found."),
            (_, existing) => existing with { Status = QuoteStatus.InProgress });

        _logger.LogInformation("Added line item {LineItemId} ({Category}: ${Amount:N2}) to quote {QuoteRequestId}",
            persisted.Id, persisted.Category, persisted.AmountAud, persisted.QuoteRequestId);
        return Task.FromResult(persisted);
    }

    /// <summary>
    /// Completes a quote request, setting its status to Completed.
    /// </summary>
    public Task<QuoteRequest> CompleteQuoteAsync(Guid quoteRequestId, CancellationToken cancellationToken = default)
    {
        if (!_requests.TryGetValue(quoteRequestId, out var existing))
            throw new InvalidOperationException($"Quote request {quoteRequestId} not found.");

        var completed = existing with { Status = QuoteStatus.Completed };
        _requests[quoteRequestId] = completed;

        _logger.LogInformation("Completed quote request {QuoteRequestId}", quoteRequestId);
        return Task.FromResult(completed);
    }
}
