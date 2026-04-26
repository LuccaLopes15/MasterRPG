namespace T20Master.Web.Data.Entities;

public class CharacterSheet
{
    public Guid Id { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Race { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int XP { get; set; }
    public int CopperCoins { get; set; }
    public int SilverCoins { get; set; }
    public int GoldCoins { get; set; }

    // Atributos
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;

    // Combate
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int MaxMP { get; set; }
    public int CurrentMP { get; set; }
    public int Defense { get; set; }
    public decimal Speed { get; set; } = 9m;

    // JSON columns
    public string Classes { get; set; } = "[]";
    public string Skills { get; set; } = "[]";
    public string Abilities { get; set; } = "[]";
    public string Spells { get; set; } = "[]";
    public string Equipment { get; set; } = "[]";

    public string? Notes { get; set; }

    public ApplicationUser Player { get; set; } = null!;
    public Campaign Campaign { get; set; } = null!;
}
