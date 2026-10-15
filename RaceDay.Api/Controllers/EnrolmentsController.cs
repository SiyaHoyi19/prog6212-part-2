using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api")]
public class EnrolmentsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    static EnrolmentDto D(Enrolment x) => new(x.EnrolmentId, x.UserId, x.EventId, x.CategoryId, x.Date, x.Status);
    [HttpPost("events/{eventId:int}/enrolments"), SwaggerOperation(Summary = "Enrol the current Participant in an event using a category")]
    public async Task<IActionResult> Create(int eventId, CreateEnrolmentRequest r)
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        if (eventId != r.EventId)
            return BadRequest(new
            {
                message = "Enrol for an event and choose a category"
            });
        var ev = await Db.Events.FindAsync(eventId);
        var cat = await Db.Categories.FindAsync(r.CategoryId);
        if (ev is null || cat is null || cat.EventId != eventId)
            return NotFound(new
            {
                message = "Event or category not found for this event."
            });
        if (await Db.Enrolments.AnyAsync(x => x.UserId == CurrentUserId && x.EventId == eventId))
            return Conflict(new
            {
                message = "Already enrolled in this event."
            });
        var x = new Enrolment { UserId = CurrentUserId!.Value, EventId = eventId, CategoryId = r.CategoryId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Pending" };
        Db.Enrolments.Add(x);
        await Db.SaveChangesAsync();
        return Created("/api/users/enrolments", D(x));
    }
    [HttpGet("users/enrolments"), SwaggerOperation(Summary = "View own participant's enrolments")]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        return Ok(await Db.Enrolments.Where(x => x.UserId == CurrentUserId).Select(x => new EnrolmentDto(x.EnrolmentId, x.UserId, x.EventId, x.CategoryId, x.Date, x.Status)).ToListAsync());
    }
    [HttpGet("events/{eventId:int}/enrolments"), SwaggerOperation(Summary = "Organiser views enrolments for their own event")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return Forbid();
        return Ok(await Db.Enrolments.Where(x => x.EventId == eventId).Select(x => new EnrolmentDto(x.EnrolmentId, x.UserId, x.EventId, x.CategoryId, x.Date, x.Status)).ToListAsync());
    }
    [HttpPut("enrolments/{id:int}/status"), SwaggerOperation(Summary = "Update enrolment status for your own event")]
    public async Task<IActionResult> Status(int id, UpdateEnrolmentStatusRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (!new[] { "Pending", "Confirmed", "Cancelled" }.Contains(r.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest();
        var x = await Db.Enrolments.Include(x => x.Event).SingleOrDefaultAsync(x => x.EnrolmentId == id);
        if (x is null)
            return NotFound();
        if (x.Event!.OrganiserId != CurrentUserId)
            return Forbid();
        x.Status = char.ToUpperInvariant(r.Status[0]) + r.Status[1..].ToLowerInvariant();
        await Db.SaveChangesAsync();
        return Ok(D(x));
    }
}
