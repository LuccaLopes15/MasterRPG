namespace T20Master.Web.Data.Entities;

public enum CampaignStatus
{
    Active,
    Paused,
    Finished
}

public class Campaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string MasterId { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; } = CampaignStatus.Active;

    public ApplicationUser Master { get; set; } = null!;
    public List<CampaignPlayer> Players { get; set; } = [];
    public List<CharacterSheet> Characters { get; set; } = [];
    public List<ChatHistory> ChatHistories { get; set; } = [];
    public List<GameSession> GameSessions { get; set; } = [];
}
