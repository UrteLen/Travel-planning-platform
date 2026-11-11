using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Enums;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class ChecklistService
{
    private readonly TravelPlannerDbContext _db;

    public ChecklistService(TravelPlannerDbContext db)
    {
        _db = db;
    }

    public async Task<ChecklistResponse> GetAsync(Guid tripId, Guid userId)
    {
        var participantId = await FindParticipantIdAsync(tripId, userId);

        var items = await _db.ChecklistItems
            .AsNoTracking()
            .Where(item => item.ParticipantId == participantId)
            .ToListAsync();

        return BuildResponse(items);
    }

    public async Task<ChecklistItemResponse> AddItemAsync(
        Guid tripId,
        Guid userId,
        string text,
        ChecklistCategory category = ChecklistCategory.Other)
    {
        var participantId = await FindParticipantIdAsync(tripId, userId);
        var item = new ChecklistItem(participantId, text, category);

        var alreadyExists = await _db.ChecklistItems.AnyAsync(existing =>
            existing.ParticipantId == participantId
            && existing.Category == item.Category
            && existing.Text.ToLower() == item.Text.ToLower());

        if (alreadyExists)
        {
            throw new DuplicateChecklistItemException(item.Text);
        }

        _db.ChecklistItems.Add(item);
        await _db.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task<ChecklistItemResponse> SetDoneAsync(
        Guid tripId,
        Guid userId,
        Guid itemId,
        bool isDone)
    {
        var item = await FindOwnItemAsync(tripId, userId, itemId);

        if (isDone)
        {
            item.MarkDone();
        }
        else
        {
            item.MarkNotDone();
        }

        await _db.SaveChangesAsync();

        return ToResponse(item);
    }

    public async Task RemoveItemAsync(Guid tripId, Guid userId, Guid itemId)
    {
        var item = await FindOwnItemAsync(tripId, userId, itemId);

        _db.ChecklistItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    public static int CalculatePercent(int doneCount, int totalCount)
    {
        if (totalCount == 0)
        {
            return 0;
        }

        return (int)Math.Round(doneCount * 100m / totalCount, MidpointRounding.AwayFromZero);
    }

    public static ChecklistResponse BuildResponse(IEnumerable<ChecklistItem> items)
    {
        var itemList = items.ToList();

        var groups = itemList
            .GroupBy(item => item.Category)
            .OrderBy(group => group.Key)
            .Select(group => new ChecklistGroupResponse(
                group.Key,
                group
                    .OrderBy(item => item.IsDone)
                    .ThenBy(item => item.Text)
                    .Select(ToResponse)
                    .ToList()))
            .ToList();

        var doneCount = itemList.Count(item => item.IsDone);

        return new ChecklistResponse(
            groups,
            doneCount,
            itemList.Count,
            CalculatePercent(doneCount, itemList.Count));
    }

    private async Task<Guid> FindParticipantIdAsync(Guid tripId, Guid userId)
    {
        var participantId = await _db.Trips
            .Where(trip => trip.Id == tripId)
            .SelectMany(trip => trip.Participants)
            .Where(participant => participant.UserId == userId)
            .Select(participant => (Guid?)participant.Id)
            .FirstOrDefaultAsync();

        if (participantId is null)
        {
            throw new NotTripParticipantException(tripId);
        }

        return participantId.Value;
    }

    private async Task<ChecklistItem> FindOwnItemAsync(Guid tripId, Guid userId, Guid itemId)
    {
        var participantId = await FindParticipantIdAsync(tripId, userId);

        var item = await _db.ChecklistItems.FirstOrDefaultAsync(candidate =>
            candidate.Id == itemId && candidate.ParticipantId == participantId);

        return item ?? throw new ChecklistItemNotFoundException(itemId);
    }

    private static ChecklistItemResponse ToResponse(ChecklistItem item)
    {
        return new ChecklistItemResponse(item.Id, item.Text, item.Category, item.IsDone);
    }
}