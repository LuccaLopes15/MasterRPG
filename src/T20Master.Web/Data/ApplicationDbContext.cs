using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data.Entities;

namespace T20Master.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignPlayer> CampaignPlayers => Set<CampaignPlayer>();
    public DbSet<CharacterSheet> CharacterSheets => Set<CharacterSheet>();
    public DbSet<RuleChunk> RuleChunks => Set<RuleChunk>();
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        // Campaign
        modelBuilder.Entity<Campaign>(e =>
        {
            e.HasIndex(c => c.InviteCode).IsUnique();
            e.HasOne(c => c.Master)
                .WithMany()
                .HasForeignKey(c => c.MasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CampaignPlayer
        modelBuilder.Entity<CampaignPlayer>(e =>
        {
            e.HasKey(cp => new { cp.CampaignId, cp.PlayerId });
            e.HasOne(cp => cp.Campaign)
                .WithMany(c => c.Players)
                .HasForeignKey(cp => cp.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cp => cp.Player)
                .WithMany()
                .HasForeignKey(cp => cp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CharacterSheet
        modelBuilder.Entity<CharacterSheet>(e =>
        {
            e.HasOne(cs => cs.Campaign)
                .WithMany(c => c.Characters)
                .HasForeignKey(cs => cs.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cs => cs.Player)
                .WithMany()
                .HasForeignKey(cs => cs.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(cs => cs.Classes).HasColumnType("jsonb");
            e.Property(cs => cs.Skills).HasColumnType("jsonb");
            e.Property(cs => cs.Abilities).HasColumnType("jsonb");
            e.Property(cs => cs.Spells).HasColumnType("jsonb");
            e.Property(cs => cs.Equipment).HasColumnType("jsonb");
        });

        // RuleChunk
        modelBuilder.Entity<RuleChunk>(e =>
        {
            e.Property(r => r.Embedding).HasColumnType("vector(3072)");
        });

        // ChatHistory
        modelBuilder.Entity<ChatHistory>(e =>
        {
            e.HasOne(ch => ch.Campaign)
                .WithMany(c => c.ChatHistories)
                .HasForeignKey(ch => ch.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ch => ch.User)
                .WithMany()
                .HasForeignKey(ch => ch.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(ch => ch.Messages).HasColumnType("jsonb");
        });
    }
}
