using Pgvector;

namespace T20Master.Web.Data.Entities;

public class RuleChunk
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
    public int SourcePage { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
}
