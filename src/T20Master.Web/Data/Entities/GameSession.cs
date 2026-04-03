namespace T20Master.Web.Data.Entities;

public enum SessionStatus
{
    Active,
    Paused,
    Ended
}

public enum SceneMode
{
    Exploration,
    Combat
}

public class GameSession
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public SceneMode Mode { get; set; } = SceneMode.Exploration;
    public int GridWidth { get; set; } = 40;
    public int GridHeight { get; set; } = 40;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public List<MapToken> Tokens { get; set; } = [];
    public List<CombatModifier> CombatModifiers { get; set; } = [];
}
