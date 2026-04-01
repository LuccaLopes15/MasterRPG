namespace T20Master.Web.Data.Entities;

public class ChatHistory
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Messages { get; set; } = "[]";

    public Campaign Campaign { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
