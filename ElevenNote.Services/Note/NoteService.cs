using System.Security.Claims;
using ElevenNote.Data;
using ElevenNote.Data.Entities;
using ElevenNote.Models.Note;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ElevenNote.Services.Note;

public class NoteService : INoteService
{
    private readonly int _userId;
    private readonly ApplicationDbContext _dbContext;
    public NoteService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext, IConfiguration config)
    {
        var userClaims = httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;
        var identifierClaimType = config["ClaimTypes:Id"] ?? "Id";
        var value = userClaims?.FindFirst(identifierClaimType)?.Value;
        var validId = int.TryParse(value, out _userId);
        if (!validId)
            throw new Exception("Attempted to build NoteService without User Id claim.");

        _dbContext = dbContext;
    }

    public async Task<NoteListItem?> CreateNoteAsync(NoteCreate request)
    {
        var noteEntity = new NoteEntity
        {
            Title = request.Title,
            Content = request.Content,
            CreatedUtc = DateTimeOffset.Now,
            OwnerId = _userId
        };

        _dbContext.Notes.Add(noteEntity);
        var numberOfChanges = await _dbContext.SaveChangesAsync();

        if (numberOfChanges == 1)
        {
            NoteListItem response = new()
            {
                Id = noteEntity.Id,
                Title = noteEntity.Title,
                CreatedUtc = noteEntity.CreatedUtc
            };
            return response;
        }

        return null;
    }

    public async Task<IEnumerable<NoteListItem>> GetAllNotesAsync()
    {
        var notes = await _dbContext.Notes
            .Where(entity => entity.OwnerId == _userId)
            .Select(entity => new NoteListItem
            {
                Id = entity.Id,
                Title = entity.Title,
                CreatedUtc = entity.CreatedUtc
            })
            .ToListAsync();

        return notes;
    }

    public async Task<NoteDetail?> GetNoteByIdAsync(int noteId)
    {
        // Find the first note that has the given Id
        // and an OwnerId that matches the requesting _userId
        var noteEntity = await _dbContext.Notes
            .FirstOrDefaultAsync(e =>
                e.Id == noteId && e.OwnerId == _userId
            );

        // If noteEntity is null then return null
        // Otherwise initialize and return a new NoteDetail
        return noteEntity is null ? null : new NoteDetail
        {
            Id = noteEntity.Id,
            Title = noteEntity.Title,
            Content = noteEntity.Content,
            CreatedUtc = noteEntity.CreatedUtc,
            ModifiedUtc = noteEntity.ModifiedUtc
        };
    }

    public async Task<bool> UpdateNoteAsync(NoteUpdate request)
    {
        // Find the note and validate it's owned by the user
        var noteEntity = await _dbContext.Notes.FindAsync(request.Id);

        // By using the null conditioanl operator we can check if it's null
        // And at the same time we check the OwnerId vs the _userId
        if (noteEntity?.OwnerId != _userId)
            return false;

        // Now we update the entity's properties
        noteEntity.Title = request.Title;
        noteEntity.Content = request.Content;
        noteEntity.ModifiedUtc = DateTimeOffset.Now;

        // Save teh changes to the database and capture how many rows were updated
        var numberOfChanges = await _dbContext.SaveChangesAsync();

        // numberOfChanges is stated to be equal to 1 because only one row is updated
        return numberOfChanges == 1;
    }

    public async Task<bool> DeleteNoteAsync(int noteId)
    {
        // Find the note by the given Id
        var noteEntity = await _dbContext.Notes.FindAsync(noteId);

        // Validate the note exists and is owned by the user
        if (noteEntity?.OwnerId != _userId)
            return false;

        // Remove the note from the DbContext and assert that the one change was saved
        _dbContext.Notes.Remove(noteEntity);
        return await _dbContext.SaveChangesAsync() == 1;
    }
}