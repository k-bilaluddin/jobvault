namespace JobVault.Application.Common;

public class ApplicationIngestionResult
{
    public bool IsSuccess { get; init; }
    public string? ApplicationId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApplicationIngestionResult Success(string applicationId) => new()
    {
        IsSuccess = true,
        ApplicationId = applicationId
    };

    public static ApplicationIngestionResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
