using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using T20Master.Web.Hubs;

namespace T20Master.Web.Services;

public class GameSessionService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<CampaignSessionHub> _hub;

    public GameSessionService(ApplicationDbContext db, IHubContext<CampaignSessionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<GameSession> StartSessionAsync(Guid campaignId, string masterId, int gridWidth = 40, int gridHeight = 40)
    {
        var campaign = await _db.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        // Encerrar sessao ativa se houver
        var existing = await _db.GameSessions
            .FirstOrDefaultAsync(gs => gs.CampaignId == campaignId && gs.Status == SessionStatus.Active);
        if (existing != null)
        {
            existing.Status = SessionStatus.Ended;
            existing.EndedAt = DateTime.UtcNow;
        }

        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Status = SessionStatus.Active,
            Mode = SceneMode.Exploration,
            GridWidth = Math.Clamp(gridWidth, 5, 40),
            GridHeight = Math.Clamp(gridHeight, 5, 40),
            StartedAt = DateTime.UtcNow
        };

        _db.GameSessions.Add(session);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{campaignId}")
            .SendAsync("SessionStarted", session.Id, session.GridWidth, session.GridHeight);

        return session;
    }

    public async Task EndSessionAsync(Guid sessionId, string masterId)
    {
        var session = await _db.GameSessions
            .Include(gs => gs.Campaign)
            .FirstOrDefaultAsync(gs => gs.Id == sessionId);

        if (session == null || session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        session.Status = SessionStatus.Ended;
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{session.CampaignId}")
            .SendAsync("SessionEnded");
    }

    public async Task<GameSession?> GetActiveSessionAsync(Guid campaignId)
    {
        return await _db.GameSessions
            .Include(gs => gs.Tokens).ThenInclude(t => t.CharacterSheet)
            .Include(gs => gs.CombatModifiers)
            .FirstOrDefaultAsync(gs => gs.CampaignId == campaignId && gs.Status == SessionStatus.Active);
    }

    public async Task SetModeAsync(Guid sessionId, string masterId, SceneMode mode)
    {
        var session = await _db.GameSessions
            .Include(gs => gs.Campaign)
            .FirstOrDefaultAsync(gs => gs.Id == sessionId);

        if (session == null || session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        session.Mode = mode;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{session.CampaignId}")
            .SendAsync("ModeChanged", mode.ToString());
    }

    public async Task ResizeGridAsync(Guid sessionId, string masterId, int width, int height)
    {
        var session = await _db.GameSessions
            .Include(gs => gs.Campaign)
            .FirstOrDefaultAsync(gs => gs.Id == sessionId);

        if (session == null || session.Campaign.MasterId != masterId)
            throw new UnauthorizedAccessException();

        session.GridWidth = Math.Clamp(width, 5, 40);
        session.GridHeight = Math.Clamp(height, 5, 40);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"campaign:{session.CampaignId}")
            .SendAsync("GridResized", session.GridWidth, session.GridHeight);
    }
}
