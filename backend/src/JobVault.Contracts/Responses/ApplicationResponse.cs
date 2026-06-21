namespace JobVault.Contracts.Responses;

public class ApplicationResponse
{
    public string Name { get; init; } = "";
    public string Synced_at { get; init; } = "";
    public bool Has_report { get; init; }
    public bool Has_notes { get; init; }
    public bool Has_cv_pdf { get; init; }
    public bool Has_letter_pdf { get; init; }
    public int? Match_pct { get; init; }
    public string Recommend { get; init; } = "";
    public string Job_url { get; init; } = "";
    public string Role { get; init; } = "";
    public bool Applied { get; init; }
    public string Applied_date { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Personal_notes { get; init; } = "";
    public List<InterviewResponse> Interviews { get; init; } = [];
    public SalaryResponse Salary { get; init; } = new();
    public RecruiterResponse Recruiter { get; init; } = new();
    public string Follow_up_date { get; init; } = "";
    public string Source { get; init; } = "";
}

public class InterviewResponse
{
    public int Id { get; init; }
    public string Date { get; init; } = "";
    public string Type { get; init; } = "";
    public string Notes { get; init; } = "";
    public string Outcome { get; init; } = "";
}

public class SalaryResponse
{
    public string Advertised { get; init; } = "";
    public string Target { get; init; } = "";
    public string Discussed { get; init; } = "";
    public string Offered { get; init; } = "";
}

public class RecruiterResponse
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Linkedin { get; init; } = "";
}
