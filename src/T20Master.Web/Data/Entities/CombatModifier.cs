namespace T20Master.Web.Data.Entities;

public enum ModifierTarget
{
    Defense,
    Speed,
    MaxHP,
    MaxMP,
    CurrentHP,
    CurrentMP
}

public class CombatModifier
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    // Para PCs (nullable para permitir modifiers em inimigos)
    public Guid? CharacterSheetId { get; set; }

    // Para Enemies (nullable para permitir modifiers em PCs)
    public Guid? MapTokenId { get; set; }

    public ModifierTarget Target { get; set; }
    public int Value { get; set; }
    public string Source { get; set; } = string.Empty;

    public GameSession Session { get; set; } = null!;
    public CharacterSheet? CharacterSheet { get; set; }
    public MapToken? MapToken { get; set; }
}
