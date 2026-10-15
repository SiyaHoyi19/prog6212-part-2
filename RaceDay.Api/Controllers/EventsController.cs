using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    static EventDto D(Event x) => new(x.EventId, x.OrganiserId, x.Name, x.Description, x.Date, x.Location, x.Distance, x.Type);
    [HttpGet, SwaggerOperation(Summary = "view all events (authenticated users)")]
    public async Task<IActionResult> GetAll()
    {
        if (RequireLogin() is IActionResult e)
            return e;
        return Ok(await Db.Events.Select(x => new EventDto(x.EventId, x.OrganiserId, x.Name, x.Description, x.Date, x.Location, x.Distance, x.Type)).ToListAsync());
    }
    [HttpGet("{id:int}"), SwaggerOperation(Summary = "View one event")]
    public async Task<IActionResult> Get(int id)
    {
        if (RequireLogin() is IActionResult e)
            return e;
        var x = await Db.Events.FindAsync(id);
        return x is null ? NotFound() : Ok(D(x));
    }
    [HttpGet("mine"), SwaggerOperation(Summary = "View all events created by the logged-in Organiser")]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        return Ok(await Db.Events.Where(x => x.OrganiserId == CurrentUserId).Select(x => new EventDto(x.EventId, x.OrganiserId, x.Name, x.Description, x.Date, x.Location, x.Distance, x.Type)).ToListAsync());
    }
    [HttpPost, SwaggerOperation(Summary = "Create an event (Organiser only)")]
    public async Task<IActionResult> Create(CreateEventRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (!ModelState.IsValid || r.Distance <= 0 || !ValidType(r.Type))
            return BadRequest(new
            {
                message = "Valid name, date, location, positive distance and Run/Walk/Cycle type required."
            });
        var x = new Event { OrganiserId = CurrentUserId!.Value, Name = r.Name.Trim(), Description = r.Description, Date = r.Date, Location = r.Location.Trim(), Distance = r.Distance, Type = Norm(r.Type) };
        Db.Events.Add(x);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new
        {
            id = x.EventId
        }, D(x));
    }
    [HttpPut("{id:int}"), SwaggerOperation(Summary = "Update an event ")]
    public async Task<IActionResult> Update(int id, UpdateEventRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (!ModelState.IsValid || r.Distance <= 0 || !ValidType(r.Type))
            return BadRequest();
        var x = await Db.Events.FindAsync(id);
        if (x is null)
            return NotFound();
        if (x.OrganiserId != CurrentUserId)
            return Forbid();
        x.Name = r.Name.Trim();
        x.Description = r.Description;
        x.Date = r.Date;
        x.Location = r.Location.Trim();
        x.Distance = r.Distance;
        x.Type = Norm(r.Type);
        await Db.SaveChangesAsync();
        return Ok(D(x));
    }
    [HttpDelete("{id:int}"), SwaggerOperation(Summary = "Delete an event owned by the current Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var x = await Db.Events.FindAsync(id);
        if (x is null)
            return NotFound();
        if (x.OrganiserId != CurrentUserId)
            return Forbid();
        Db.Events.Remove(x);
        await Db.SaveChangesAsync();
        return Ok(new
        {
            message = "Event deleted."
        });
    }
    private static bool ValidType(string t) => new[] { "run", "walk", "cycle" }.Contains(t, StringComparer.OrdinalIgnoreCase); private static string Norm(string t) => char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant();
}
