using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using T20Master.Web.Data.Models;

namespace T20Master.Web.Services;

public class CharacterService
{
    private readonly ApplicationDbContext _db;

    public CharacterService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CharacterSheet> CreateCharacterAsync(Guid campaignId, string playerId, string name, string race, string className)
    {
        var character = new CharacterSheet
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            PlayerId = playerId,
            Name = name,
            Race = race,
            Level = 1,
            Classes = JsonSerializer.Serialize(new[] { new ClassEntry { ClassName = className, Level = 1 } }),
            Skills = JsonSerializer.Serialize(T20Reference.Skills.Select(s => new SkillEntry { Name = s }).ToArray()),
            Abilities = "[]",
            Spells = "[]",
            Equipment = "[]"
        };

        _db.CharacterSheets.Add(character);
        await _db.SaveChangesAsync();
        return character;
    }

    public async Task<CharacterSheet?> GetCharacterAsync(Guid characterId, string userId)
    {
        var character = await _db.CharacterSheets
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null) return null;
        if (character.PlayerId != userId && character.Campaign.MasterId != userId) return null;

        return character;
    }

    public async Task<List<CharacterSheet>> GetCharactersForCampaignAsync(Guid campaignId, string userId)
    {
        var campaign = await _db.Campaigns.FindAsync(campaignId);
        if (campaign == null) return [];

        if (campaign.MasterId == userId)
        {
            return await _db.CharacterSheets
                .Where(c => c.CampaignId == campaignId)
                .Include(c => c.Player)
                .ToListAsync();
        }

        return await _db.CharacterSheets
            .Where(c => c.CampaignId == campaignId && c.PlayerId == userId)
            .ToListAsync();
    }

    public async Task<CharacterSheet?> GetMyCharacterAsync(Guid campaignId, string playerId)
    {
        return await _db.CharacterSheets
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId && c.PlayerId == playerId);
    }

    public async Task<bool> UpdateCharacterAsync(Guid characterId, string userId, CharacterSheet updated)
    {
        _db.ChangeTracker.Clear();

        var character = await _db.CharacterSheets
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null) return false;
        if (character.PlayerId != userId && character.Campaign.MasterId != userId) return false;

        character.Name = updated.Name;
        character.Race = updated.Race;
        character.Level = updated.Level;
        character.XP = updated.XP;
        character.CopperCoins = updated.CopperCoins;
        character.SilverCoins = updated.SilverCoins;
        character.GoldCoins = updated.GoldCoins;
        character.Strength = updated.Strength;
        character.Dexterity = updated.Dexterity;
        character.Constitution = updated.Constitution;
        character.Intelligence = updated.Intelligence;
        character.Wisdom = updated.Wisdom;
        character.Charisma = updated.Charisma;
        character.MaxHP = updated.MaxHP;
        character.CurrentHP = updated.CurrentHP;
        character.MaxMP = updated.MaxMP;
        character.CurrentMP = updated.CurrentMP;
        character.Defense = updated.Defense;
        character.Speed = updated.Speed;
        character.Classes = updated.Classes;
        character.Skills = updated.Skills;
        character.Abilities = updated.Abilities;
        character.Spells = updated.Spells;
        character.Equipment = updated.Equipment;
        character.Notes = updated.Notes;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId, string userId)
    {
        var character = await _db.CharacterSheets
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null) return false;
        if (character.PlayerId != userId && character.Campaign.MasterId != userId) return false;

        _db.CharacterSheets.Remove(character);
        await _db.SaveChangesAsync();
        return true;
    }
}
