using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using T20Master.Web.Hubs;

namespace T20Master.Web.Services;

public class MapService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<CampaignSessionHub> _hub;

    public MapService(ApplicationDbContext db, IHubContext<CampaignSessionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<MapToken> PlaceTokenAsync(Guid sessionId, string masterId, MapToken token)
    {
        var session = await GetSessionAndValidateMaster(sessionId, masterId);

        token.Id = Guid.NewGuid();
        token.SessionId = sessionId;
        token.Session = null!;

        _db.ChangeTracker.Clear();
        _db.MapTokens.Add(token);
        await _db.SaveChangesAsync();

        // Recarregar com CharacterSheet se for PC
        if (token.CharacterSheetId.HasValue)
        {
            await _db.Entry(token).Reference(t => t.CharacterSheet).LoadAsync();
        }

        await _hub.Clients.Group($"campaign:{session.CampaignId}")
            .SendAsync("TokenAdded", new
            {
                token.Id,
                token.SessionId,
                Type = token.Type.ToString(),
                token.GridX,
                token.GridY,
                token.CharacterSheetId,
                CharacterName = token.CharacterSheet?.Name,
                token.Label,
                token.MaxHP,
                token.CurrentHP,
                token.Speed,
                token.Color
            });

        return token;
    }

    public async Task MoveTokenAsync(Guid tokenId, string masterId, int newX, int newY)
    {
        _db.ChangeTracker.Clear();

        var token = await _db.MapTokens
            .Include(t => t.Session).ThenInclude(s => s.Campaign)
            .FirstOrDefaultAsync(t => t.Id == tokenId);

        if (token == null || token.Session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        token.GridX = newX;
        token.GridY = newY;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{token.Session.CampaignId}")
            .SendAsync("TokenMoved", tokenId, newX, newY);
    }

    public async Task RemoveTokenAsync(Guid tokenId, string masterId)
    {
        _db.ChangeTracker.Clear();

        var token = await _db.MapTokens
            .Include(t => t.Session).ThenInclude(s => s.Campaign)
            .FirstOrDefaultAsync(t => t.Id == tokenId);

        if (token == null || token.Session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        var campaignId = token.Session.CampaignId;
        _db.MapTokens.Remove(token);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("TokenRemoved", tokenId);
    }

    public async Task ClearMapAsync(Guid sessionId, string masterId)
    {
        var session = await GetSessionAndValidateMaster(sessionId, masterId);

        await _db.MapTokens.Where(t => t.SessionId == sessionId).ExecuteDeleteAsync();

        await _hub.Clients.Group($"campaign:{session.CampaignId}")
            .SendAsync("MapCleared");
    }

    private async Task<GameSession> GetSessionAndValidateMaster(Guid sessionId, string masterId)
    {
        var session = await _db.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Campaign)
            .FirstOrDefaultAsync(gs => gs.Id == sessionId);

        if (session == null || session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        return session;
    }
}
