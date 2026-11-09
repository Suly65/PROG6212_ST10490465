# RaceDay – API Endpoint Plan (Part 2 implementation)

This is the Part 1 endpoint plan (`docs/part1-original/API_Endpoint_Plan.pdf`) **as implemented in Part 2**.
Every Part 1 endpoint is implemented. Rows marked **Changed** or **Added** are the deliberate deviations explained in the README ("Deviations from Part 1").

Authentication is **session based**: `POST /api/auth/login` stores `UserId` and `Role` in a server-side session and returns a session cookie.
`401 Unauthorized` = not logged in. `403 Forbidden` = logged in with the wrong role, or not the owner of the resource.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates an Organiser or Participant account. Password is hashed (PBKDF2). | None (public) | `{ fullName, email, password, role }` | `201` user (no password). `400` invalid fields / invalid role. `409` email already registered. | Unchanged |
| POST | `/api/auth/login` | Verifies credentials and creates the server-side session (UserId + Role). | None (public) | `{ email, password }` | `200` `{ userId, fullName, email, role, message }` + session cookie. `400` invalid fields. `401` invalid credentials. | **Changed** – session cookie instead of `{ token, role }` |
| POST | `/api/auth/logout` | Clears the session. | None (public) | None | `200` `{ message }` | **Added** |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| GET | `/api/users/me` | Profile of the logged-in user. | Any (logged in) | None | `200` profile. `401`. | Unchanged |
| PUT | `/api/users/me` | Updates the logged-in user's own name and email. | Any (logged in) | `{ fullName, email }` | `200` updated profile. `400`. `401`. `409` email used by another account. | Unchanged |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| GET | `/api/events` | Lists events (ordered by date). Optional `search`, `eventType`, `upcomingOnly` query parameters. | None (public) | None | `200` array of events. `400` invalid `eventType`. | Unchanged (filters specified) |
| GET | `/api/events/mine` | Events created by the logged-in Organiser. | Organiser | None | `200` array. `401`. `403`. | **Added** |
| GET | `/api/events/{id}` | One event including its categories (and routes). | None (public) | None | `200` event. `404`. | Unchanged |
| POST | `/api/events` | Creates an event owned by the logged-in Organiser. | Organiser | `{ name, description, eventDate, location, distanceKm, eventType }` | `201` event. `400`. `401`. `403`. | **Changed** – `distanceKm` and `eventType` (Run/Walk/Cycle) added |
| PUT | `/api/events/{id}` | Updates an event owned by the logged-in Organiser. | Organiser | `{ name, description, eventDate, location, distanceKm, eventType }` | `200` event. `400`. `401`. `403` not owner. `404`. | **Changed** – same new fields |
| DELETE | `/api/events/{id}` | Deletes an owned event (and its categories, enrolments, results). | Organiser | None | `200` confirmation. `401`. `403`. `404`. | Unchanged |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Categories of an event. | None (public) | None | `200` array. `404` event not found. | Unchanged |
| POST | `/api/events/{eventId}/categories` | Adds a category to an owned event. | Organiser | `{ name, distanceKm, price }` | `201` category. `400`. `401`. `403`. `404`. | Unchanged |
| PUT | `/api/categories/{id}` | Updates a category of an owned event. | Organiser | `{ name, distanceKm, price }` | `200` category. `400`. `401`. `403`. `404`. | Unchanged |
| DELETE | `/api/categories/{id}` | Removes a category of an owned event. | Organiser | None | `200` confirmation. `401`. `403`. `404`. | Unchanged |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| POST | `/api/enrolments` | Enrols the logged-in Participant in a category (links Participant, Category and Event). | Participant | `{ categoryId }` | `201` enrolment. `400` invalid / already enrolled. `401`. `403` not a Participant. `404` category not found. | Unchanged (403 documented) |
| GET | `/api/enrolments/me` | The logged-in Participant's own enrolments. | Participant | None | `200` array. `401`. `403`. | Unchanged |
| GET | `/api/events/{eventId}/enrolments` | All enrolments for an owned event. | Organiser | None | `200` array. `401`. `403` not owner. `404`. | Unchanged |
| DELETE | `/api/enrolments/{id}` | Cancels the Participant's own enrolment (Status → Cancelled). | Participant | None | `200` confirmation. `401`. `403` not owner. `404`. | Unchanged |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response | Part 1 status |
|---|---|---|---|---|---|---|
| POST | `/api/results` | Captures finish time and position for an enrolment of an owned event. | Organiser | `{ enrolmentId, finishTime, position }` | `201` result. `400` invalid / duplicate / cancelled enrolment. `401`. `403` not owner. `404` enrolment not found. | Unchanged |
| GET | `/api/results/me` | The logged-in Participant's own results. | Participant | None | `200` array. `401`. `403`. | Unchanged |
| GET | `/api/events/{eventId}/results` | Leaderboard for an event (by position, then time). | None (public) | None | `200` array. `404`. | Unchanged |
