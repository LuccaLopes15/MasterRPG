using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;

namespace T20Master.Web.Services;

public class CampaignService
{
    private readonly ApplicationDbContext _db;

    public CampaignService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Campaign> CreateCampaignAsync(string userId, string name, string? description)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            MasterId = userId,
            InviteCode = Guid.NewGuid(),
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
        return campaign;
    }

    public async Task<List<Campaign>> GetCampaignsForUserAsync(string userId)
    {
        var asMaster = await _db.Campaigns
            .Where(c => c.MasterId == userId)
            .Include(c => c.Players)
            .ToListAsync();

        var asPlayer = await _db.Campaigns
            .Where(c => c.Players.Any(p => p.PlayerId == userId))
            .Include(c => c.Players)
            .ToListAsync();

        return asMaster.Union(asPlayer).ToList();
    }

    public async Task<Campaign?> GetCampaignAsync(Guid campaignId, string userId)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Master)
            .Include(c => c.Players).ThenInclude(p => p.Player)
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return null;
        if (campaign.MasterId != userId && !campaign.Players.Any(p => p.PlayerId == userId))
            return null;

        return campaign;
    }

    public async Task<bool> JoinCampaignAsync(Guid inviteCode, string userId)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Players)
            .FirstOrDefaultAsync(c => c.InviteCode == inviteCode);

        if (campaign == null) return false;
        if (campaign.MasterId == userId) return true;
        if (campaign.Players.Any(p => p.PlayerId == userId)) return true;

        _db.CampaignPlayers.Add(new CampaignPlayer
        {
            CampaignId = campaign.Id,
            PlayerId = userId,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Campaign?> GetCampaignByInviteCodeAsync(Guid inviteCode)
    {
        return await _db.Campaigns.FirstOrDefaultAsync(c => c.InviteCode == inviteCode);
    }

    public async Task<bool> UpdateStatusAsync(Guid campaignId, string userId, CampaignStatus status)
    {
        var campaign = await _db.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.MasterId != userId) return false;

        campaign.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    public bool IsMaster(Campaign campaign, string userId) => campaign.MasterId == userId;

    public bool IsMember(Campaign campaign, string userId) =>
        campaign.MasterId == userId || campaign.Players.Any(p => p.PlayerId == userId);
}
