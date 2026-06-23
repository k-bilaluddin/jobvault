namespace JobVault.Contracts.Errors;

public class AppException : Exception
{
    public string ErrorCode { get; }
    public object[] Args { get; }

    public AppException(string errorCode, params object[] args)
        : base(ErrorCatalog.Get(errorCode).Title)
    {
        ErrorCode = errorCode;
        Args = args;
    }
}
