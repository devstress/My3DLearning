using Terranes.Contracts.Models;

namespace Terranes.Contracts.Abstractions;

/// <summary>
/// Service for requesting and aggregating end-to-end quotes from partners.
/// </summary>
public interface IQuotingService
{
    /// <summary>
    /// Submits a new quote request for a site placement.
    /// </summary>
    /// <param name="request">The quote request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created quote request with its assigned identifier.</returns>
    Task<QuoteRequest> RequestQuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current state of a quote request including all received line items.
    /// </summary>
    /// <param name="quoteRequestId">The quote request identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The quote request, or <c>null</c> if not found.</returns>
    Task<QuoteRequest?> GetQuoteRequestAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all quote line items for a given quote request.
    /// </summary>
    /// <param name="quoteRequestId">The quote request identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All line items associated with the quote request.</returns>
    Task<IReadOnlyList<QuoteLineItem>> GetLineItemsAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a line item from a partner to an existing quote request.
    /// </summary>
    Task<QuoteLineItem> AddLineItemAsync(QuoteLineItem lineItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a quote request, setting its status to Completed.
    /// </summary>
    Task<QuoteRequest> CompleteQuoteAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);
}
