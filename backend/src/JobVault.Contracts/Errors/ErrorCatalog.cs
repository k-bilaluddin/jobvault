using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.Contracts.Errors;

public static class ErrorCatalog
{
    private static readonly Dictionary<string, ApiError> Errors = new()
    {
        // Auth
        ["auth.invalid_credentials"]        = new(401, "Invalid Credentials",       "Email or password is incorrect"),
        ["auth.token_expired"]              = new(401, "Token Expired",             "Authentication token has expired"),
        ["auth.token_invalid"]              = new(401, "Token Invalid",             "Token validation failed"),

        // Application
        ["application.not_found"]           = new(404, "Application Not Found",     "No application found for '{0}'"),
        ["application.stage_update_failed"] = new(404, "Stage Update Failed",       "Failed to update stage for '{0}'"),
        ["application.notes_update_failed"] = new(404, "Notes Update Failed",       "Failed to update notes for '{0}'"),
        ["application.interview_add_failed"]= new(404, "Interview Add Failed",      "Failed to add interview for '{0}'"),
        ["application.interview_del_failed"]= new(404, "Interview Delete Failed",   "Failed to delete interview for '{0}'"),

        // Ingest
        ["ingest.validation_failed"]        = new(400, "Validation Failed",         "Required field '{0}' is missing"),
        ["ingest.empty_company_name"]       = new(400, "Empty Company Name",        "Company name is required"),
        ["ingest.upsert_failed"]            = new(500, "Upsert Failed",             "Failed to persist application for '{0}'"),
        ["ingest.publish_failed"]           = new(500, "Publish Failed",            "Failed to publish event for '{0}'"),

        // Vault
        ["vault.file_not_found"]            = new(404, "File Not Found",            "No {0} file found for '{1}'"),
        ["vault.company_required"]          = new(400, "Company Required",          "Company name is required"),
        ["vault.no_files_uploaded"]         = new(400, "No Files Uploaded",         "At least one file must be uploaded"),
        ["vault.empty_files"]              = new(400, "Empty Files",                "All uploaded files are empty"),
        ["vault.sync_failed"]              = new(502, "Sync Failed",                "Git sync failed: {0}"),

        // Processing
        ["processing.application_missing"]  = new(404, "Application Missing",       "Application not found during processing"),
        ["processing.conversion_failed"]    = new(500, "Conversion Failed",         "LibreOffice PDF conversion failed for '{0}'"),
        ["processing.commit_failed"]        = new(500, "Commit Failed",             "GitHub commit failed: {0}"),
        ["processing.generation_failed"]    = new(502, "Generation Failed",         "Document generation service returned error"),

        // Config
        ["config.missing_required"]         = new(500, "Missing Configuration",     "Required configuration '{0}' is not set"),
        ["config.invalid_value"]            = new(500, "Invalid Configuration",     "Configuration '{0}' is invalid: {1}"),

        // Server
        ["server.internal_error"]           = new(500, "Internal Server Error",     "An unexpected error occurred"),
    };

    private static readonly Dictionary<ApiError, string> ReverseMap =
        Errors.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static ApiError Get(string code) =>
        Errors.TryGetValue(code, out var error)
            ? error
            : throw new ArgumentException($"Unknown error code: {code}");

    public static string GetCode(ApiError error) =>
        ReverseMap.TryGetValue(error, out var code) ? code : "server.internal_error";

    public static ProblemDetails ToProblem(string code, HttpContext context, params object[] args) =>
        Get(code).ToProblemDetails(context, args);
}
