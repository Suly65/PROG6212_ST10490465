/* =====================================================================
   RaceDay Database Schema
   PROG6212 - Programming 2B - POE Part 1
   Run this script on a fresh SQL Server instance in SSMS.
   ===================================================================== */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if they already exist (child tables first, for re-runs) */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ---------------------------------------------------------------------
   Users  (holds both Organisers and Participants; Role distinguishes)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL DEFAULT 'Participant'
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ---------------------------------------------------------------------
   Events  (created by an Organiser)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    Name            NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(MAX)   NULL,
    EventDate       DATETIME        NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

/* ---------------------------------------------------------------------
   Categories  (e.g. 5km, 10km, 21km for a given Event)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    Name            NVARCHAR(50)    NOT NULL,
    DistanceKM      DECIMAL(5,2)    NOT NULL,
    Price           DECIMAL(8,2)    NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

/* ---------------------------------------------------------------------
   Routes  (one route per Category)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Routes (
    RouteID         INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT             NOT NULL UNIQUE,
    RouteName       NVARCHAR(100)   NOT NULL,
    ElevationGainM  INT             NOT NULL DEFAULT 0,
    MapURL          NVARCHAR(255)   NULL,
    CONSTRAINT FK_Routes_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID)
);
GO

/* ---------------------------------------------------------------------
   Enrolments  (a Participant entering a Category)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed','Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID)
);
GO

/* ---------------------------------------------------------------------
   Results  (one Result per Enrolment)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NOT NULL,
    Position        INT             NULL,
    RecordedAt      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* =====================================================================
   Seed data
   ===================================================================== */

-- 2 Organisers + 2 Participants
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Nkosi',    'thabo.organiser@raceday.co.za',   'HASHED_PW_1', 'Organiser'),
('Lindiwe Mokoena', 'lindiwe.organiser@raceday.co.za', 'HASHED_PW_2', 'Organiser'),
('Sarah van Wyk',   'sarah.participant@raceday.co.za', 'HASHED_PW_3', 'Participant'),
('Johan Botha',     'johan.participant@raceday.co.za', 'HASHED_PW_4', 'Participant');
GO

-- 3 Events
INSERT INTO dbo.Events (OrganiserID, Name, Description, EventDate, Location) VALUES
(1, 'Gqeberha Bay Run',      'Scenic coastal road running event.', '2026-10-18 07:00', 'Gqeberha, Eastern Cape'),
(1, 'Nelson Mandela Bay Cycle Tour', 'Community charity cycling event.', '2026-11-08 06:30', 'Gqeberha, Eastern Cape'),
(2, 'Joburg Park Walk & Run', 'Family-friendly park run and walk.', '2026-09-27 08:00', 'Johannesburg, Gauteng');
GO

-- Categories for each event
INSERT INTO dbo.Categories (EventID, Name, DistanceKM, Price) VALUES
(1, '5km Fun Run',   5.00,  50.00),
(1, '10km Race',     10.00, 100.00),
(1, '21km Half Marathon', 21.10, 150.00),
(2, '40km Cycle',    40.00, 200.00),
(2, '80km Cycle',    80.00, 300.00),
(3, '3km Family Walk', 3.00, 0.00),
(3, '10km Park Run',  10.00, 60.00);
GO

-- Routes for each category
INSERT INTO dbo.Routes (CategoryID, RouteName, ElevationGainM, MapURL) VALUES
(1, 'Beachfront Loop',        30,  'https://maps.example.com/route1'),
(2, 'Humewood to Summerstrand', 60, 'https://maps.example.com/route2'),
(3, 'Coastal Half Marathon Route', 120, 'https://maps.example.com/route3'),
(4, 'Northern Areas Loop',    250, 'https://maps.example.com/route4'),
(5, 'Extended Cycle Circuit', 480, 'https://maps.example.com/route5'),
(6, 'Zoo Lake Family Path',   10,  'https://maps.example.com/route6'),
(7, 'Zoo Lake Extended Path', 25,  'https://maps.example.com/route7');
GO

-- Sample enrolments (participants entering categories)
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, Status) VALUES
(3, 2, 'Confirmed'),   -- Sarah -> 10km Race
(3, 6, 'Confirmed'),   -- Sarah -> 3km Family Walk
(4, 3, 'Confirmed'),   -- Johan -> 21km Half Marathon
(4, 4, 'Confirmed');   -- Johan -> 40km Cycle
GO

-- Sample results for some enrolments
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position) VALUES
(1, '00:52:30', 14),
(3, '01:48:12', 3);
GO
