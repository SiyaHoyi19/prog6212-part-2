# RaceDay — PROG6212 Part 2 RESTful API

**Student reference:** ST10485019 · **Stack:** ASP.NET Core Web API (.NET 8), C#, EF Core Code First, SQL Server, xUnit, Swagger, GitHub Actions.

RaceDay supports community run, walk and cycle events. This Part 2 API extends the Part 1 planning documents retained under `docs/` and uses the model breakdown from the Part 2 guide: `Role`, `AppUser`, `Event`, `Category`, `Enrolment`, and `Result`.

> **Before submission:** run the solution on your own machine, confirm the tests and SQL Server connection, push it to your own GitHub repository, capture a real green Actions screenshot, record your own unlisted YouTube walkthrough, and add the links below. Those account-specific submission artefacts cannot honestly be generated from this ZIP.

## User roles

- **Organiser:** manage only their own events and categories; review enrolments for events they own; update enrolment status; record and view results for their own events.
- **Participant:** browse events/categories; update their own profile; enrol in events and select a category; view only their own enrolments and results.
- Both roles can browse events/categories and update their own profile. Registration accepts only `Organiser` or `Participant`.

## Repository structure

```text
RaceDay.Api/                 Extracted solution folder
├── RaceDay.sln
├── RaceDay.Api/
│   ├── Controllers/
│   ├── Data/
│   │   └── Migrations/
│   ├── Models/
│   │   ├── AppUser.cs
│   │   ├── Category.cs
│   │   ├── Event.cs
│   │   ├── Enrolment.cs
│   │   ├── Result.cs
│   │   └── Role.cs
│   └── Services/
├── RaceDay.Contracts/       Shared request/response DTOs
├── RaceDay.Api.Tests/       Automated integration tests
├── docs/                    Part 1 ERD, endpoint plan and SQL script
└── .github/workflows/ci.yml Build-and-test workflow
```

## Prerequisites

- Visual Studio 2022 with the .NET 8 SDK/workload, or .NET 8 SDK and CLI
- SQL Server Express/Developer and SQL Server Management Studio (SSMS)
- Git

## Database setup (EF Core Code First)

1. Start your local SQL Server instance. The default connection string in `RaceDay.Api/appsettings.json` targets `localhost\SQLEXPRESS`, database `RaceDay` and Windows integrated security. Change `ConnectionStrings:DefaultConnection` for your SQL Server instance if necessary. Do not commit credentials.
2. From the repository root, install the EF CLI if needed: `dotnet tool install --global dotnet-ef --version 8.0.11`.
3. Apply the initial migration already included under `RaceDay.Api/Data/Migrations`:

   ```bash
   dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
   ```

   If `dotnet-ef` is not installed, run the global-tool install command above first. This applies `InitialRaceDayDatabase` and creates the six tables and their keys, indexes and relationships. Inspect the tables in SSMS. The database is created from the C# models; the Part 1 SQL file is retained as the original design artefact.
4. The Part 1 SQL script is retained unchanged as a planning artefact. The Part 2 migration is the executable schema source and follows the guide's six-model breakdown. It stores credentials with `AppUser`, uses `Role` as a lookup table, and includes generated keys, unique email/username constraints, event details and enrolment status.

The six models are stored separately under `RaceDay.Api/Models` and configured in `RaceDay.Api/Data/RaceDayDbContext.cs`. One role can have many users; each user can organise events and make enrolments. Password hashes are stored in `AppUser.PasswordHash`, never the original password.

To create a later schema change, update the model and `RaceDayDbContext`, then run `dotnet ef migrations add <MigrationName> --project RaceDay.Api --startup-project RaceDay.Api` from the repository root. Review the generated migration before applying it with `dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api`.

## Run the API

```bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project RaceDay.Api
```

Open the HTTPS URL shown by the console and append `/swagger` (the launch profile uses `https://localhost:7043/swagger`). In Swagger, register an account, log in, then use the session cookie for subsequent requests. Swagger's same browser session should retain the cookie; if testing from a separate client, preserve the `RaceDay.Session` cookie. Sessions expire after 30 minutes of inactivity. Logout clears the session.

### Example registration JSON

```json
{
  "fullName": "Demo Organiser",
  "email": "organiser@example.com",
  "password": "Use-A-Strong-Password-123!",
  "phoneNumber": "0712345678",
  "dateOfBirth": "1990-01-01",
  "role": "Organiser",
  "username": "demoorganiser"
}
```

## Endpoint reference

All endpoints are documented in Swagger. Protected requests return `401` without a session and `403` when the signed-in role is not allowed.

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register account; PBKDF2 password hash |
| POST | `/api/auth/login` | Public | Verify credentials and create session |
| POST | `/api/auth/logout` | Session | Clear session |
| GET / PUT | `/api/profile/me` | Both roles | View/update own profile only |
| GET | `/api/events` | Both roles | Browse events |
| GET | `/api/events/{id}` | Both roles | View one event |
| GET | `/api/events/mine` | Organiser | List own events |
| POST | `/api/events` | Organiser | Create event |
| PUT / DELETE | `/api/events/{id}` | Owning Organiser | Update/delete own event |
| GET | `/api/events/{eventId}/categories` | Both roles | List categories |
| POST | `/api/events/{eventId}/categories` | Owning Organiser | Create category |
| PUT / DELETE | `/api/categories/{id}` | Owning Organiser | Update/delete category |
| POST | `/api/events/{eventId}/enrolments` | Participant | Enrol with a category |
| GET | `/api/users/enrolments` | Participant | View own enrolments |
| GET | `/api/events/{eventId}/enrolments` | Owning Organiser | View event enrolments |
| PUT | `/api/enrolments/{id}/status` | Owning Organiser | Set Pending/Confirmed/Cancelled |
| POST | `/api/enrolments/{enrolmentId}/results` | Owning Organiser | Capture finish time, position and status |
| GET | `/api/user/results` | Participant | View own results |
| GET | `/api/events/{eventId}/results` | Owning Organiser | View event results |

Event type must be `Run`, `Walk` or `Cycle`. An event organiser can only edit/delete their own events. Categories, enrolments and results are checked against event ownership before organiser changes are permitted. Duplicate enrolment for the same participant/event is rejected.

## Unit tests

Run locally:

```bash
dotnet test RaceDay.sln --configuration Release
```

`RaceDay.Api.Tests` uses `WebApplicationFactory` and EF Core InMemory for isolated integration testing. Tests cover unauthenticated profile access, successful registration/login, duplicate registration, invalid credentials, role rejection, organiser event creation and participant enrolment/duplicate prevention. Run the tests locally before pushing; CI runs restore, build and tests on pushes and pull requests.

## CI/CD evidence

Workflow: `.github/workflows/ci.yml`. It restores, builds and runs tests using .NET 8. Once your own GitHub Actions run succeeds, save a screenshot of the green workflow and place it at `docs/ci-green-build.png`, then uncomment/update the image below.

<!-- Replace this note with a screenshot captured from your own successful GitHub Actions run. Do not use a fabricated build screenshot. -->

**GitHub Actions screenshot:** _Add `docs/ci-green-build.png` after the workflow passes in your repository._

## Video presentation

**Unlisted YouTube URL:** _Add your own video link here after recording._

Suggested walkthrough: repository structure; Part 1 ERD and entity relationships; DbContext and Code-First migration; registration and password hashing; login/session cookie and logout; organiser event/category management; participant enrolment; role rejection; results; Swagger; Test Explorer/`dotnet test`; and the green GitHub Actions workflow.

## GitHub commit history and submission checklist

This ZIP is a working source snapshot; it does not fabricate a GitHub account history. Work in your own repository/branch and commit each meaningful development step as you complete it. The brief requires at least 20 meaningful commits, a real green CI screenshot, an unlisted video and submission of the repository URL on ARC. Add the actual links and evidence before submitting.

## Design notes

The API follows the Part 2 guide's six-model breakdown and keeps the original Part 1 planning files unchanged. The EF Core migration is the Part 2 schema source; see `docs/Part2-implementation-notes.md` for the model and role relationship details.
