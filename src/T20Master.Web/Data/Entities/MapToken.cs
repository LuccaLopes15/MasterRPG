namespace T20Master.Web.Data.Entities;

public enum TokenType
{
    PlayerCharacter,
    Enemy,
    NPC,
    Obstacle
}

public class MapToken
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public TokenType Type { get; set; }

    public int GridX { get; set; }
    public int GridY { get; set; }

    // Para tokens de PlayerCharacter
    public Guid? CharacterSheetId { get; set; }

    // Para tokens de Enemy/NPC (dados inline)
    public string? Label { get; set; }
    public int? MaxHP { get; set; }
    public int? CurrentHP { get; set; }
    public decimal? Speed { get; set; }
    public string? Color { get; set; }

    public GameSession Session { get; set; } = null!;
    public CharacterSheet? CharacterSheet { get; set; }
}
