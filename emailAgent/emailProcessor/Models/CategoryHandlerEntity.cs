using Azure;
using Azure.Data.Tables;

namespace EmailProcessor.Models;

/// <summary>
/// Azure table entity representing a category handler for email processing
/// </summary>
public class CategoryHandlerEntity : ITableEntity
{
    /// <summary>
    /// Partition key for Azure Table Storage
    /// </summary>
    public string PartitionKey { get; set; } = "DigitalValet";

    /// <summary>
    /// Row key (GUID) for Azure Table Storage
    /// </summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp for Azure Table Storage
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// ETag for Azure Table Storage optimistic concurrency
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Email category/classification
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Handler URL for this email category
    /// </summary>
    public string HandlerUrl { get; set; } = string.Empty;
}
