namespace T20Master.Web.Data.Models;

public class ClassEntry
{
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
}

public class SkillEntry
{
    public string Name { get; set; } = string.Empty;
    public bool Trained { get; set; }
    public int Bonus { get; set; }
    public string Specialty { get; set; } = string.Empty;
}

public class AbilityEntry
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SpellEntry
{
    public string Name { get; set; } = string.Empty;
    public int Circle { get; set; } = 1;
    public string School { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EquipmentEntry
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Bonus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
