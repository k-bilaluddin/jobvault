namespace JobVault.Domain.ValueObjects;

public class RolePayload
{
    /// <summary>
    /// One of: calvergy | senior_baris | developer_baris | junior_baris
    /// </summary>
    public string Id { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = [];
}
