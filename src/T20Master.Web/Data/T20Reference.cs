namespace T20Master.Web.Data;

public static class T20Reference
{
    public sealed record SkillDefinition(string Name, string DisplayName, string Attribute);

    public static readonly string[] Races =
    [
        "Humano", "Anao", "Dahllan", "Elfo", "Goblin", "Lefou",
        "Minotauro", "Qareen", "Golem", "Hynne", "Kliren",
        "Medusa", "Osteon", "Sereia/Tritao", "Sulfure", "Trog"
    ];

    public static readonly string[] Classes =
    [
        "Arcanista", "Barbaro", "Bardo", "Bucaneiro", "Cacador",
        "Cavaleiro", "Clerigo", "Druida", "Guerreiro", "Inventor",
        "Ladino", "Lutador", "Nobre", "Paladino"
    ];

    public static readonly string[] SkillAttributes = ["FOR", "DES", "CON", "INT", "SAB", "CAR"];

    public static readonly SkillDefinition[] SkillDefinitions =
    [
        new("Atletismo", "Atletismo", "FOR"),
        new("Luta", "Luta", "FOR"),

        new("Acrobacia", "Acrobacia", "DES"),
        new("Cavalgar", "Cavalgar", "DES"),
        new("Furtividade", "Furtividade", "DES"),
        new("Iniciativa", "Iniciativa", "DES"),
        new("Ladinagem", "Ladinagem", "DES"),
        new("Pilotagem", "Pilotagem", "DES"),
        new("Pontaria", "Pontaria", "DES"),
        new("Reflexos", "Reflexos", "DES"),

        new("Fortitude", "Fortitude", "CON"),

        new("Conhecimento", "Conhecimento", "INT"),
        new("Guerra", "Guerra", "INT"),
        new("Investigacao", "Investigação", "INT"),
        new("Misticismo", "Misticismo", "INT"),
        new("Nobreza", "Nobreza", "INT"),
        new("Oficio", "Ofício", "INT"),

        new("Cura", "Cura", "SAB"),
        new("Intuicao", "Intuição", "SAB"),
        new("Percepcao", "Percepção", "SAB"),
        new("Religiao", "Religião", "SAB"),
        new("Sobrevivencia", "Sobrevivência", "SAB"),
        new("Vontade", "Vontade", "SAB"),

        new("Adestramento", "Adestramento", "CAR"),
        new("Atuacao", "Atuação", "CAR"),
        new("Diplomacia", "Diplomacia", "CAR"),
        new("Enganacao", "Enganação", "CAR"),
        new("Intimidacao", "Intimidação", "CAR"),
        new("Jogatina", "Jogatina", "CAR")
    ];

    public static readonly string[] Skills = SkillDefinitions.Select(skill => skill.Name).ToArray();
}
