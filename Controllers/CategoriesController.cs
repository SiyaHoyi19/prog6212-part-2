using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api")]
public class CategoriesController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpGet("events/{eventId:int}/categories"), SwaggerOperation(Summary = "View  categories for an event")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (RequireLogin() is IActionResult e)
            return e;
        if (!await Db.Events.AnyAsync(x => x.EventId == eventId))
            return NotFound();
        return Ok(await Db.Categories.Where(x => x.EventId == eventId).Select(x => new CategoryDto(x.CategoryId, x.EventId, x.Name, x.Distance, x.MinAge)).ToListAsync());
    }
    [HttpPost("events/{eventId:int}/categories"), SwaggerOperation(Summary = "Create a category for an event owned by the Organiser")]
    public async Task<IActionResult> Create(int eventId, CreateCategoryRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return Forbid();
        if (!ModelState.IsValid || r.MinAge < 0)
            return BadRequest();
        var c = new Category { EventId = eventId, Name = r.Name.Trim(), Distance = r.Distance.Trim(), MinAge = r.MinAge };
        Db.Categories.Add(c);
        await Db.SaveChangesAsync();
        return Created($"/api/categories/{c.CategoryId}", new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Distance, c.MinAge));
    }
    [HttpPut("categories/{id:int}"), SwaggerOperation(Summary = "Update a category")]
    public async Task<IActionResult> Update(int id, CreateCategoryRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var c = await Db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.CategoryId == id);
        if (c is null)
            return NotFound();
        if (c.Event!.OrganiserId != CurrentUserId)
            return Forbid();
        if (!ModelState.IsValid || r.MinAge < 0)
            return BadRequest();
        c.Name = r.Name.Trim();
        c.Distance = r.Distance.Trim();
        c.MinAge = r.MinAge;
        await Db.SaveChangesAsync();
        return Ok(new CategoryDto(c.CategoryId, c.EventId, c.Name, c.Distance, c.MinAge));
    }
    [HttpDelete("categories/{id:int}"), SwaggerOperation(Summary = "Delete a category")]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var c = await Db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.CategoryId == id);
        if (c is null)
            return NotFound();
        if (c.Event!.OrganiserId != CurrentUserId)
            return Forbid();
        Db.Categories.Remove(c);
        await Db.SaveChangesAsync();
        return Ok(new
        {
            message = "Category deleted."
        });
    }
}
