namespace T20Master.Web.Data.Entities;

public class InviteCode
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
}
