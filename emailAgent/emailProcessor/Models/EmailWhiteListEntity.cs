using Azure;
using Azure.Data.Tables;

namespace EmailProcessor.Models;

/// <summary>
/// Azure table entity representing an email address in the whitelist
/// </summary>
public class EmailWhiteListEntity : ITableEntity
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
    /// Display name for the email address
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address to check against
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include emails from this address in daily reports
    /// </summary>
    public bool IncludeInReport { get; set; }

    /// <summary>
    /// Optional handler URL for this email address
    /// </summary>
    public string? HandlerUrl { get; set; }
}
