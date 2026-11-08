-- SmartShop T05 design draft. Run on a fresh SQLite database.
-- Assumptions to confirm: orders are simulated kiosk orders, and auth
-- covers staff accounts. Align Users and Roles with the T13 auth design.
-- Supply timestamps as UTC ISO 8601 text from the application.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS EventLog (
    EventId INTEGER PRIMARY KEY,
    DeviceId INTEGER NOT NULL,
    Timestamp TEXT NOT NULL,
    Value REAL NOT NULL,
    Unit TEXT NOT NULL,
    Status TEXT NOT NULL CHECK (Status IN ('Normal', 'Warning', 'Critical')),
    IsDataAvailable INTEGER NOT NULL CHECK (IsDataAvailable IN (0, 1))
);

CREATE TABLE IF NOT EXISTS AuditTrail (
    AuditId INTEGER PRIMARY KEY,
    AdminUserId TEXT NOT NULL,
    Action TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    DeviceId INTEGER,
    RequestedState TEXT,
    PreviousState TEXT,
    ConfirmedState TEXT,
    IsSuccess INTEGER CHECK (IsSuccess IN (0, 1)),
    ErrorMessage TEXT,
    CHECK (
        Action <> 'AlarmCommand' OR (
            DeviceId IS NOT NULL AND RequestedState IS NOT NULL
            AND PreviousState IS NOT NULL AND IsSuccess IS NOT NULL
            AND (IsSuccess = 0 OR ConfirmedState IS NOT NULL)
        )
    )
);

-- AuditTrail retains the actor ID independently of account changes.
CREATE TABLE IF NOT EXISTS Roles (
    RoleId INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Users (
    UserId TEXT PRIMARY KEY NOT NULL,
    Email TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash TEXT NOT NULL,
    RoleId INTEGER NOT NULL REFERENCES Roles (RoleId)
);

-- One record per simulated kiosk order. No payment card fields.
CREATE TABLE IF NOT EXISTS Orders (
    OrderId INTEGER PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    Status TEXT NOT NULL,
    TotalCents INTEGER NOT NULL CHECK (TotalCents >= 0)
);

