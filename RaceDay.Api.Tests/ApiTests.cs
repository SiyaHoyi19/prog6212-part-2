using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Xunit;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase("RaceDayTests_" + Guid.NewGuid()));
        });
    }
}

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpointWithoutSessionReturns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAndLoginWorkAndDuplicateEmailConflicts()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        var request = new RegisterRequest(
            "Test Organiser",
            "org@example.com",
            "StrongPassword123!",
            "0712345678",
            new DateOnly(1990, 1, 1),
            "Organiser",
            "orgtest");

        var registration = await client.PostAsJsonAsync(
            "/api/authentication/register", request);

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var duplicate = await client.PostAsJsonAsync(
            "/api/authentication/register", request);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest("org@example.com", "StrongPassword123!"));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task InvalidCredentialsAreRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest("missing@example.com", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotCreateEventAndOrganiserCan()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        await Register(client, "participant@example.com", "Participant", "parttest");
        await Login(client, "participant@example.com");

        var eventRequest = new CreateEventRequest(
            "Test race", "Test", new DateOnly(2027, 5, 1),
            "Gqeberha", 5, "Run");

        var participantResponse =
            await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Forbidden, participantResponse.StatusCode);

        await client.PostAsync("/api/authentication/logout", content: null);

        await Register(client, "organiser@example.com", "Organiser", "orgtest2");
        await Login(client, "organiser@example.com");

        var organiserResponse =
            await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Created, organiserResponse.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrolAndDuplicateIsRejected()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = true });

        await Register(client, "organiser2@example.com", "Organiser", "orgtest3");
        await Login(client, "organiser2@example.com");

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new CreateEventRequest(
                "Test race", "Test", new DateOnly(2027, 5, 1),
                "Gqeberha", 5, "Run"));

        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);

        var eventJson = await eventResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(eventJson.TryGetProperty("eventId", out var eventIdValue),
            "The event response did not contain eventId.");
        var eventId = eventIdValue.GetInt32();

        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new CreateCategoryRequest("5km", "5km", 16));

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var categoryJson =
            await categoryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(categoryJson.TryGetProperty("categoryId", out var categoryIdValue),
            "The category response did not contain categoryId.");
        var categoryId = categoryIdValue.GetInt32();

        await client.PostAsync("/api/authentication/logout", content: null);

        await Register(client, "participant2@example.com", "Participant", "parttest2");
        await Login(client, "participant2@example.com");

        var enrolmentRequest = new CreateEnrolmentRequest(eventId, categoryId);

        var enrolmentResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", enrolmentRequest);

        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments", enrolmentRequest);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    private static async Task Register(
        HttpClient client, string email, string role, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/api/authentication/register",
            new RegisterRequest(
                "Test User",
                email,
                "StrongPassword123!",
                "0712345678",
                new DateOnly(1995, 1, 1),
                role,
                username));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/authentication/login",
            new LoginRequest(email, "StrongPassword123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
