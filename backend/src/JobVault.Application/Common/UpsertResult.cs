namespace JobVault.Application.Common;

/// <summary>
/// Represents the result of an upsert operation.
/// </summary>
public class UpsertResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets whether a new document was created (as opposed to updated).
    /// </summary>
    public bool IsNewDocument { get; init; }

    /// <summary>
    /// Gets the MongoDB ObjectId of the upserted document (stringified).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Creates a successful upsert result.
    /// </summary>
    /// <param name="isNewDocument">Whether a new document was created.</param>
    /// <param name="id">The MongoDB ObjectId of the document.</param>
    /// <returns>A successful result.</returns>
    public static UpsertResult Success(bool isNewDocument, string? id = null) => new()
    {
        IsSuccess = true,
        IsNewDocument = isNewDocument,
        Id = id
    };

    /// <summary>
    /// Creates a failed upsert result.
    /// </summary>
    /// <returns>A failed result.</returns>
    public static UpsertResult Failure() => new() 
    { 
        IsSuccess = false, 
        IsNewDocument = false 
    };
}