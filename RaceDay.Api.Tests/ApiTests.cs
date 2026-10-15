using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Xunit;
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Development");
        b.ConfigureServices(s => { var d = s.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<RaceDayDbContext>)); if (d != null) s.Remove(d); s.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase("RaceDayTests_" + Guid.NewGuid())); });
    }
}
public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory f; public ApiTests(ApiFactory factory)
    {
        f = f;
    }
    [Fact]
    public async Task ProtectedEndpointWithoutSessionReturns401()
    {
        var c = f.CreateClient();
        var r = await c.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
    [Fact]
    public async Task RegistrationAndLoginWorkAndDuplicateEmailConflicts()
    {
        var c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var dto = new RegisterRequest("Test Organiser", "org@example.com", "StrongPassword123!", "0712345678", new DateOnly(1990, 1, 1), "Organiser", "orgtest");
        var a = await c.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        var duplicate = await c.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var login = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("org@example.com", "StrongPassword123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
    [Fact]
    public async Task InvalidCredentialsAreRejected()
    {
        var c = f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("missing@example.com", "wrongpassword"));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
    [Fact]
    public async Task ParticipantCannotCreateEventAndOrganiserCan()
    {
        var c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await Register(c, "participant@example.com", "Participant", "parttest");
        await Login(c, "participant@example.com");
        var eventBody = new CreateEventRequest("Test race", "Test", new DateOnly(2027, 5, 1), "Gqeberha", 5, "Run");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/events", eventBody)).StatusCode);
        await c.PostAsync("/api/auth/logout", null);
        await Register(c, "organiser@example.com", "Organiser", "orgtest2");
        await Login(c, "organiser@example.com");
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/events", eventBody)).StatusCode);
    }
    [Fact]
    public async Task ParticipantCanEnrolAndDuplicateIsRejected()
    {
        var c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await Register(c, "organiser2@example.com", "Organiser", "orgtest3");
        await Login(c, "organiser2@example.com");
        var ev = await c.PostAsJsonAsync("/api/events", new CreateEventRequest("Test race", "Test", new DateOnly(2027, 5, 1), "Gqeberha", 5, "Run"));
        var eventJson = await ev.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        int eventId = eventJson!["eventId"].GetInt32();
        var categoryResponse = await c.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest("5km", "5km", 16));
        var categoryJson = await categoryResponse.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        int categoryId = categoryJson!["categoryId"].GetInt32();
        await c.PostAsync("/api/auth/logout", null);
        await Register(c, "participant2@example.com", "Participant", "parttest2");
        await Login(c, "participant2@example.com");
        var body = new CreateEnrolmentRequest(eventId, categoryId);
        var enrolled = await c.PostAsJsonAsync($"/api/events/{eventId}/enrolments", body);
        Assert.Equal(HttpStatusCode.Created, enrolled.StatusCode);
        var duplicate = await c.PostAsJsonAsync($"/api/events/{eventId}/enrolments", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }
    private static async Task Register(HttpClient c, string email, string role, string username) => await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Test User", email, "StrongPassword123!", "0712345678", new DateOnly(1995, 1, 1), role, username)); private static async Task Login(HttpClient c, string email) => await c.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "StrongPassword123!"));
}
