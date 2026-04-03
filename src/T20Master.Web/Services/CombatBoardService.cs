using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using T20Master.Web.Hubs;

namespace T20Master.Web.Services;

public class CombatBoardService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<CampaignSessionHub> _hub;

    public CombatBoardService(ApplicationDbContext db, IHubContext<CampaignSessionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task UpdateStatAsync(Guid characterId, string masterId, string field, int value, Guid campaignId)
    {
        _db.ChangeTracker.Clear();
        var character = await _db.CharacterSheets
            .Include(cs => cs.Campaign)
            .FirstOrDefaultAsync(cs => cs.Id == characterId);

        if (character == null || character.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        switch (field)
        {
            case "CurrentHP": character.CurrentHP = value; break;
            case "CurrentMP": character.CurrentMP = value; break;
            case "Defense": character.Defense = value; break;
            case "Speed": character.Speed = value; break;
            case "MaxHP": character.MaxHP = value; break;
            case "MaxMP": character.MaxMP = value; break;
            default: throw new ArgumentException($"Invalid field: {field}");
        }

        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("StatsUpdated", characterId, field, value);
    }

    public async Task UpdateEnemyStatAsync(Guid tokenId, string masterId, string field, int value, Guid campaignId)
    {
        _db.ChangeTracker.Clear();
        var token = await _db.MapTokens
            .Include(t => t.Session).ThenInclude(s => s.Campaign)
            .FirstOrDefaultAsync(t => t.Id == tokenId);

        if (token == null || token.Session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        switch (field)
        {
            case "CurrentHP": token.CurrentHP = value; break;
            case "MaxHP": token.MaxHP = value; break;
            default: throw new ArgumentException($"Invalid field: {field}");
        }

        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("EnemyStatsUpdated", tokenId, field, value);
    }

    public async Task<CombatModifier> AddModifierAsync(CombatModifier modifier, string masterId, Guid campaignId)
    {
        _db.ChangeTracker.Clear();
        var campaign = await _db.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        modifier.Id = Guid.NewGuid();
        _db.CombatModifiers.Add(modifier);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("ModifierAdded", new
            {
                modifier.Id,
                modifier.SessionId,
                modifier.CharacterSheetId,
                modifier.MapTokenId,
                Target = modifier.Target.ToString(),
                modifier.Value,
                modifier.Source
            });

        return modifier;
    }

    public async Task RemoveModifierAsync(Guid modifierId, string masterId, Guid campaignId)
    {
        _db.ChangeTracker.Clear();
        var modifier = await _db.CombatModifiers
            .Include(cm => cm.Session).ThenInclude(s => s.Campaign)
            .FirstOrDefaultAsync(cm => cm.Id == modifierId);

        if (modifier == null || modifier.Session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        _db.CombatModifiers.Remove(modifier);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("ModifierRemoved", modifierId);
    }
}
