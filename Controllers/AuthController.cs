using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services; 
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api/authentication")]
public class AuthController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new user(organiser or participant",
        Description = "Creates an app user and stores only a salted TKFY4 password hash.")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var roleName = request.Role.Trim();
        if (roleName.Equals("organiser", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Organiser";
        }
        else if (roleName.Equals("participant", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Participant";
        }
        else
        {
            return BadRequest(new
            {
                message = "Role must be Organiser or Participant."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(user => user.Email == email))
        {
            return Conflict(new
            {
                message = "Email already registered."
            });
        }

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? email.Split('@')[0]
            : request.Username.Trim();
        if (await Db.Users.AnyAsync(user => user.Username == username))
        {
            return Conflict(new
            {
                message = "Username already in use."
            });
        }

        var role = await Db.Roles.SingleOrDefaultAsync(item => item.Name == roleName);
        if (role is null)
        {
            role = new Role { Name = roleName };
            Db.Roles.Add(role);
            await Db.SaveChangesAsync();
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            DateOfBirth = request.DateOfBirth,
            Username = username,
            PasswordHash = PasswordService.Hash(request.Password),
            RoleId = role.RoleId
        };

        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        return Created("/api/profile/me", new
        {
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email,
            role = role.Name
        });
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Log a registered user in")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await Db.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Email == request.Email.Trim().ToLowerInvariant());

        if (user?.Role is null || !PasswordService.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role.Name);
        HttpContext.Session.SetString("FullName", user.FullName);

        return Ok(new AuthResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.Name,
            "Login successful. Keep the session cookie for subsequent requests."));
    }

    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Log out and clear the current session")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new
        {
            message = "Logged out."
        });
    }
}
