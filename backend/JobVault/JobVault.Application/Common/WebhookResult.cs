namespace JobVault.Application.Common;

/// <summary>
/// Represents the result of webhook processing.
/// </summary>
public class WebhookResult
{
    /// <summary>
    /// Gets whether the webhook was processed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a message describing the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful webhook result.
    /// </summary>
    /// <param name="message">A message describing the success.</param>
    /// <returns>A successful result.</returns>
    public static WebhookResult Success(string message) => new() 
    { 
        IsSuccess = true, 
        Message = message 
    };

    /// <summary>
    /// Creates a failed webhook result.
    /// </summary>
    /// <param name="message">A message describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static WebhookResult Failure(string message) => new() 
    { 
        IsSuccess = false, 
        Message = message 
    };
}