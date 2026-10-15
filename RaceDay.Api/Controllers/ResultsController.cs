using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api")]
public class ResultsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpPost("enrolments/{enrolmentId:int}/results"), SwaggerOperation(Summary = "Capture result for an enrolment in an event you organise")]
    public async Task<IActionResult> Create(int enrolmentId, CreateResultRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (enrolmentId != r.EnrolmentId)
            return BadRequest();
        var en = await Db.Enrolments.Include(x => x.Event).SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId);
        if (en is null)
            return NotFound();
        if (en.Event!.OrganiserId != CurrentUserId)
            return Forbid();
        if (r.FinishTime < 0 || !new[] { "Completed", "DNF", "DNS", "Disqualified" }.Contains(r.ResultStatus, StringComparer.OrdinalIgnoreCase))
            return BadRequest();
        if (await Db.Results.AnyAsync(x => x.EnrolmentId == enrolmentId))
            return Conflict(new
            {
                message = "A result has already been recorded for this enrolment."
            });
        var x = new Result { EnrolmentId = enrolmentId, FinishTime = r.FinishTime, Position = r.Position, ResultStatus = r.ResultStatus };
        Db.Results.Add(x);
        await Db.SaveChangesAsync();
        return Created("/api/user/results", new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus));
    }
    [HttpGet("user/results"), SwaggerOperation(Summary = "View results for the authenticated Participant")]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        return Ok(await Db.Results.Where(x => x.Enrolment!.UserId == CurrentUserId).Select(x => new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus)).ToListAsync());
    }
    [HttpGet("events/{eventId:int}/results"), SwaggerOperation(Summary = "Organiser views results for their own event")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return Forbid();
        return Ok(await Db.Results.Where(x => x.Enrolment!.EventId == eventId).Select(x => new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus)).ToListAsync());
    }
}
