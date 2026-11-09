# RaceDay

## Project Description

RaceDay is a full-stack event management system for the South African road running, walking and cycling community. It lets Event Organisers create and manage events, categories and participant results, while Participants can browse events, enter a category, and track their personal results.

The project is built progressively across three parts:

| Part | Focus | Status |
|---|---|---|
| Part 1 | System planning and database (ERD, endpoint plan, SQL script) | Complete |
| Part 2 | RESTful API (ASP.NET Core Web API, EF Core, sessions, Swagger, tests, CI/CD) | **This submission** |
| Part 3 | MVC front end that consumes this API | Upcoming |

## User Roles

### Organiser
- Registers as an Organiser and logs in.
- Creates, updates and deletes **their own** events (name, description, date, location, distance, event type).
- Defines categories for their events (e.g. 5km, 10km, Under 20).
- Views all enrolments for their events.
- Captures finish times and finishing positions.

### Participant
- Registers as a Participant and logs in.
- Browses events and categories.
- Enters an event by selecting a category, and can cancel their own enrolment.
- Views their own enrolments and personal results.

Both roles can view and update **their own** profile. Role enforcement happens on the server for every protected endpoint (`401` when not logged in, `403` for the wrong role or for someone else's data).

## Part 1

Part 1 planning documents are kept in `/docs`:

| File | Purpose |
|---|---|
| `docs/RaceDay_ERD.png` / `.pdf` | ERD (6 entities) – **updated for Part 2**, see Deviations |
| `docs/RaceDay_Schema.sql` | SQL Server script – **updated for Part 2** so it matches the ERD and EF model |
| `docs/API_Endpoint_Plan_Part2.md` | Part 1 endpoint plan as implemented in Part 2 |
| `docs/part1-original/` | The **original, untouched** Part 1 ERD, SQL script, endpoint plan PDF, README and CI file |

## Part 2

### Technologies Used
- ASP.NET Core Web API (.NET 8, controllers)
- Entity Framework Core 8 – **Code First** – SQL Server
- Server-side **session** authentication + custom role filter (`[RequireRole]`)
- PBKDF2 password hashing (HMAC-SHA256, 100 000 iterations, per-user salt)
- Swashbuckle / Swagger UI (with annotations)
- xUnit + `WebApplicationFactory` + EF Core InMemory (tests)
- GitHub Actions (restore → build → test)

### Project Structure

```
RaceDay
├── docs/                          Part 1 planning documents (ERD, SQL script, endpoint plan)
│   └── part1-original/            Original Part 1 files, unchanged
├── RaceDay.Api/                   ASP.NET Core Web API
│   ├── Controllers/               Auth, Profile, Events, Categories, Enrolments, Results
│   ├── Data/                      RaceDayDbContext (relationships, constraints, delete behaviour)
│   ├── Filters/                   RequireRoleAttribute / RequireLoginAttribute
│   ├── Mapping/                   Model -> DTO mapping
│   ├── Models/                    AppUser, Event, Category, RaceRoute, Enrolment, Result
│   ├── Services/                  PasswordService (hashing), SessionKeys
│   ├── appsettings.json           SQL Server connection string
│   └── Program.cs                 DbContext, Session, Swagger, middleware
├── RaceDay.Contracts/             Shared request/response DTOs (Dtos.cs)
├── RaceDay.Api.Tests/             xUnit tests
├── .github/workflows/ci.yml       GitHub Actions workflow
└── README.md
```

### Database Setup

Prerequisites: .NET 8 SDK, Visual Studio 2022 and SQL Server (Express/LocalDB/Developer) with SQL Server Management Studio.

1. Open `RaceDay.sln` in Visual Studio 2022.
2. In `RaceDay.Api/appsettings.json`, check the connection string and change the server name if yours differs:
   ```json
   "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
3. Open **Tools → NuGet Package Manager → Package Manager Console**, set the *Default project* to `RaceDay.Api` and run:
   ```powershell
   Add-Migration InitialRaceDayDatabase
   Update-Database
   ```
   `Add-Migration` generates the `RaceDay.Api/Migrations` folder from the C# models and `RaceDayDbContext` (Code First); `Update-Database` creates `RaceDayDB` on your SQL Server and applies it. If the `Migrations` folder is already in the repository you only need `Update-Database`.
4. Open SSMS and confirm `RaceDayDB` contains: `Users`, `Events`, `Categories`, `Routes`, `Enrolments`, `Results` (plus `__EFMigrationsHistory`).

> **Note:** use **either** `Update-Database` **or** `docs/RaceDay_Schema.sql` to create `RaceDayDB`, not both on the same database. The SQL script is the Part 1 reference/seed script and produces the same schema as the EF model. If you ran the script first, drop `RaceDayDB` before running `Update-Database`. The seed users in the SQL script use placeholder password hashes and cannot log in – register users through the API instead.

### How to Run the API

1. Set `RaceDay.Api` as the startup project.
2. Press **F5** (or run `dotnet run --project RaceDay.Api` from the repository root).
3. Swagger UI opens at **http://localhost:5080/swagger** (or `https://localhost:7080/swagger` with the https profile).

Run the tests:
```bash
dotnet test RaceDay.sln
```
or use **Test → Test Explorer → Run All** in Visual Studio.

### Authentication

- `POST /api/auth/register` – validates the request, checks the email is unique, validates the role (`Organiser` or `Participant`), **hashes the password** and saves the user. Only the hash is stored.
- `POST /api/auth/login` – finds the user by email, verifies the password against the stored hash, then creates a **server-side session** storing `UserId` and `Role`. A session cookie is returned and sent automatically by the browser / Swagger UI.
- `POST /api/auth/logout` – clears the session.
- `[RequireRole("Organiser")]`, `[RequireRole("Participant")]` and `[RequireLogin]` filters read the session and return **401** (no session) or **403** (wrong role) before any controller code runs.
- Ownership is enforced as well: an Organiser can only modify events (and their categories, enrolments and results) that they own, and profile endpoints always use the `UserId` from the session – never an id supplied by the caller.

> Role selection: as required by the brief, a user selects Organiser or Participant when registering. In a production system Organiser accounts would be approved or invited, which is a sensible future improvement.

### API Endpoints

All 22 endpoints are visible and testable in Swagger. The full table (method, route, description, role, request body, expected response) is in [`docs/API_Endpoint_Plan_Part2.md`](docs/API_Endpoint_Plan_Part2.md).

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Profile | `GET /api/users/me`, `PUT /api/users/me` |
| Events | `GET /api/events`, `GET /api/events/mine`, `GET /api/events/{id}`, `POST /api/events`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` |
| Categories | `GET /api/events/{eventId}/categories`, `POST /api/events/{eventId}/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}` |
| Enrolments | `POST /api/enrolments`, `GET /api/enrolments/me`, `GET /api/events/{eventId}/enrolments`, `DELETE /api/enrolments/{id}` |
| Results | `POST /api/results`, `GET /api/results/me`, `GET /api/events/{eventId}/results` |

HTTP status codes used: `200`, `201`, `400`, `401`, `403`, `404`, `409`.

#### Deviations from Part 1

The Part 1 plan was the specification. These are the only deliberate differences and the reasons for them:

| # | Change | Reason |
|---|---|---|
| 1 | `Events` table gained **`DistanceKM`** and **`EventType`** (Run/Walk/Cycle); create/update event bodies include them. ERD and SQL script updated to match. | The Part 2 brief requires every event to capture distance and event type; the Part 1 design did not include them. |
| 2 | `POST /api/auth/login` returns user info and sets a **session cookie** instead of returning `{ token, role }`. | Part 2 requires server-side session management instead of tokens. |
| 3 | Added `POST /api/auth/logout`. | Required by the Part 2 brief to clear the session. |
| 4 | Added `GET /api/events/mine`. | Lets an Organiser list only their own events (Part 2 guide). |
| 5 | `GET /api/events` returns **all** events by default; `upcomingOnly=true` filters to future events. Also supports `search` and `eventType`. | Part 1 said "upcoming" with optional filters; making it a parameter keeps past events (and results) reachable. |
| 6 | Explicit **delete behaviour**: `ON DELETE CASCADE` Events → Categories → Routes/Enrolments → Results (SQL script and EF both). Users → Events and Users → Enrolments stay restricted. | Needed so `DELETE /api/events/{id}` works, and to avoid SQL Server "multiple cascade paths" errors. |
| 7 | `DELETE /api/enrolments/{id}` is a soft cancel (`Status = Cancelled`). | Keeps enrolment history; the Part 1 `Status` column already allows `Cancelled`. |
| 8 | Route parameter for nested resources is named `{eventId}` instead of `{id}`. | Same URL; clearer in Swagger. |
| 9 | The `Routes` entity exists in the model and is returned inside `CategoryDto`, but has no CRUD endpoints yet. | Part 1 planned no Route endpoints; route/weather information is used in Part 3. |
| 10 | The C# class for the Routes table is named `RaceRoute`. | Avoids a naming clash with `Microsoft.AspNetCore.Routing.Route`. The table is still `Routes`. |

### Swagger

Swagger UI (`/swagger`) lists every endpoint grouped by controller (Auth, Profile, Events, Categories, Enrolments, Results). Each endpoint has a summary, a description, the request body schema and the documented responses (including 401/403/404 where relevant).

To test a protected endpoint: call `POST /api/auth/login` once – Swagger then sends the session cookie automatically with every later request. Call `POST /api/auth/logout` to end the session.

**Suggested demo flow:** register an Organiser → login → create event → add categories → logout → register a Participant → login → enter an event (category) → view `GET /api/enrolments/me` → logout → login as Organiser → `GET /api/events/{eventId}/enrolments` → `POST /api/results` → logout → login as Participant → `GET /api/results/me`.

### Unit Testing

`RaceDay.Api.Tests` boots the real API in memory (`WebApplicationFactory<Program>`) with an EF Core in-memory database, so the real controllers, filters, sessions and password hashing are exercised without needing SQL Server.

| Test class | What it proves |
|---|---|
| `AuthenticationTests` | Valid registration (both roles); duplicate email, invalid role, short password and invalid email rejected; password stored as a hash; valid login creates a session; wrong password / unknown email → 401; logout clears the session; profile view/update; password hashing unit tests |
| `RoleEnforcementTests` | Unauthenticated → 401 on protected endpoints; Participant blocked from Organiser endpoints (create/update/delete event, create category, view enrolments, capture result) → 403; Organiser blocked from Participant endpoints → 403; Organisers cannot touch another Organiser's data → 403 |
| `EventManagementTests` | Organiser creates/updates/deletes events and categories; validation failures → 400; unknown ids → 404; both roles can view; search/filter; `events/mine` |
| `EnrolmentAndResultTests` | Participant can enrol and the enrolment is correctly recorded (participant, category, event, status); duplicate / unknown category rejected; Organiser views enrolments; cancel own enrolment only; Organiser captures results; Participant sees only their own results; duplicate/invalid results rejected; leaderboard ordering |

### CI/CD

`.github/workflows/ci.yml` runs on every push and pull request. It validates the repository structure (required files and the `/docs` folder), installs .NET 8, restores packages, builds the solution and runs all unit tests.

### GitHub Actions Screenshot

> **TODO before submitting:** replace this line with the screenshot of your green build, e.g. `![Green build](docs/ci-green-build.png)`

### Video Presentation

> **TODO before submitting:** replace this line with your **Unlisted** YouTube link: `https://youtu.be/XXXXXXXXXXX`


# Part 3 – MVC Web Application

The `RaceDay.Mvc` project is the web front end. It calls the Part 2 API over HTTP and does not access the database directly.

## Run locally

1. Start `RaceDay.Api` first and confirm that its Swagger page loads.
2. Confirm the API URL and port in `RaceDay.Mvc/appsettings.json` under `Api:BaseUrl`. The default is `http://localhost:5080/`; change it to the actual API address shown by Visual Studio.
3. Set `RaceDay.Mvc` as the startup project and run it.
4. Register an Organiser or Participant account and sign in.

The API uses its own server-side session cookie. The MVC application forwards that API cookie for authenticated requests. Keep the API running while using the MVC application.

## Docker

From the `RaceDay.Mvc` directory, build and run the MVC container: \n
```bash
docker build -t raceday-mvc .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5000/ raceday-mvc
```

The API must be running and reachable at the configured address. On Windows with Docker Desktop, `host.docker.internal` normally resolves to the host machine.

## Part 3 implementation notes

This is an initial MVC integration layer for authentication, event browsing, event creation, participant enrolments, event enrolment lists and result viewing. Review and test every workflow against the live API before submission. The API currently does not expose an Azure Blob Storage upload endpoint, profile-picture upload, profile update, event edit, category-management UI, or result-entry UI. These requirements are not represented as completed features in this package. Azure upload requires an Azure Storage account, a secret connection string stored outside source control, an API upload endpoint and database URL persistence.

## Video presentation

Add your unlisted YouTube presentation URL here after recording:

`[PASTE YOUTUBE LINK HERE]`
