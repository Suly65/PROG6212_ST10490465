# RaceDay

RaceDay is a full-stack event management system built for the South African road running, walking, and cycling community. It lets Event Organisers create and manage events, categories, and participant results, while Participants can browse events, enter categories, and track their personal result history.

This repository covers **Part 1: System Planning and Database** of the POE. No application code is written in this part — only the ERD, API endpoint plan, and SQL database script.

## Roles

- **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
- **Participant** — can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

## Repository Structure

```
/docs
  RaceDay_ERD.png          - Entity Relationship Diagram
  RaceDay_Schema.sql       - Full SQL Server database creation + seed script
  API_Endpoint_Plan.md     - Planned REST API endpoints
.github/workflows/ci.yml   - GitHub Actions workflow validating repo structure
README.md
```

## Database

The schema is built around 6 entities: Users, Events, Categories, Routes, Enrolments, and Results. Full details and cardinality are shown in `docs/RaceDay_ERD.png`; the matching creation script is `docs/RaceDay_Schema.sql`, which runs cleanly on a fresh SQL Server instance in SSMS and seeds sample Organisers, Participants, Events, Categories, and Enrolments.

The ERD includes one-to-many relationships (e.g. Organiser to Events, Event to Categories), one-to-one relationships (Category to Route, Enrolment to Result), and a many-to-many relationship between Participants and Categories (a participant can enter many categories, and a category can have many participants). Since SQL cannot implement a many-to-many relationship directly, it is resolved through the `Enrolments` junction/associative table, which holds a foreign key to both `Users` and `Categories`.

## API Plan

`docs/API_Endpoint_Plan.md` lists every planned endpoint covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results, along with the role required, request body, and expected response codes for each.

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and validates that the `/docs` folder and required planning documents are present.

**CI/CD Screenshot:**
[ADD YOUR SCREENSHOT OF THE GREEN BUILD HERE]

## Video Walkthrough

[ADD YOUR UNLISTED YOUTUBE LINK HERE] — walks through the ERD decisions, endpoint plan choices, and runs the SQL script live in SSMS.
