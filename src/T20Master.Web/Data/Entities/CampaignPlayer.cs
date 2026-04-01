namespace T20Master.Web.Data.Entities;

public class CampaignPlayer
{
    public Guid CampaignId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Campaign Campaign { get; set; } = null!;
    public ApplicationUser Player { get; set; } = null!;
}
