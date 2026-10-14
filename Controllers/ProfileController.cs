using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api/profile/me")]
public class ProfileController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    [SwaggerOperation(Summary = "View the authenticated user's own profile")]
    public async Task<IActionResult> Get()
    {
        if (RequireLogin() is IActionResult error)
        {
            return error;
        }

        var user = await Db.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == CurrentUserId);

        return user is null
            ? NotFound()
            : Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                Role = user.Role!.Name
            });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Update only the authenticated user's profile")]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        if (RequireLogin() is IActionResult error)
        {
            return error;
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null)
        {
            return NotFound();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(item => item.Email == email && item.UserId != user.UserId))
        {
            return Conflict(new
            {
                message = "Email already in use."
            });
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth;

        await Db.SaveChangesAsync();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth
        });
    }
}
