/* =====================================================================
   01_schema.sql  -  UtilitiesPortal database: schemas, tables, views, trigger

   Layout
     data  = the real tables (the API login gets NO access here)
     api   = the views the backend talks to (the API login gets access here)
   ===================================================================== */


IF DB_ID('UtilitiesPortal') IS NULL
    CREATE DATABASE UtilitiesPortal;
GO
USE UtilitiesPortal;
GO



/* ---------- Schemas ---------- */
CREATE SCHEMA data AUTHORIZATION dbo;
GO
CREATE SCHEMA api AUTHORIZATION dbo;
GO


/* =====================================================================
   TABLES (schema: data)
   ===================================================================== */

CREATE TABLE data.Users (
    Id            uniqueidentifier NOT NULL
                  CONSTRAINT PK_Users PRIMARY KEY
                  CONSTRAINT DF_Users_Id DEFAULT NEWID(),
    Email         nvarchar(256)    NOT NULL,
    PasswordHash  nvarchar(200)    NOT NULL,
    Role          nvarchar(20)     NOT NULL,
    IsActive      bit              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt     datetime2        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role  CHECK (Role IN ('Customer', 'Admin')),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%._%' AND Email NOT LIKE '% %')
);
GO

CREATE TABLE data.Customers (
    Id              uniqueidentifier NOT NULL
                    CONSTRAINT PK_Customers PRIMARY KEY
                    CONSTRAINT DF_Customers_Id DEFAULT NEWID(),
    UserId          uniqueidentifier NOT NULL,
    AccountNumber   nvarchar(20)     NOT NULL,
    FullName        nvarchar(100)    NOT NULL,
    Phone           nvarchar(10)     NOT NULL,   
    ServiceAddress  nvarchar(200)    NOT NULL,
    Region          nvarchar(30)     NOT NULL,
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES data.Users (Id),
    CONSTRAINT UQ_Customers_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Customers_AccountNumber UNIQUE (AccountNumber),
    CONSTRAINT CK_Customers_AccountNumber CHECK (LEN(LTRIM(RTRIM(AccountNumber))) > 0),
    CONSTRAINT CK_Customers_FullName CHECK (LEN(LTRIM(RTRIM(FullName))) > 0),
    CONSTRAINT CK_Customers_ServiceAddress CHECK (LEN(LTRIM(RTRIM(ServiceAddress))) > 0),
    CONSTRAINT CK_Customers_Phone CHECK (LEN(Phone) = 10 AND Phone NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Customers_Region CHECK (Region IN
        ('Bronx', 'Brooklyn', 'Manhattan', 'Queens', 'Staten Island', 'Westchester'))
);
GO

CREATE TABLE data.Meters (
    Id             uniqueidentifier NOT NULL
                   CONSTRAINT PK_Meters PRIMARY KEY
                   CONSTRAINT DF_Meters_Id DEFAULT NEWID(),
    CustomerId     uniqueidentifier NOT NULL,
    MeterNumber    nvarchar(20)     NOT NULL,
    Status         nvarchar(20)     NOT NULL CONSTRAINT DF_Meters_Status DEFAULT 'Active',
    InstalledDate  date             NOT NULL CONSTRAINT DF_Meters_InstalledDate DEFAULT CAST(SYSUTCDATETIME() AS date),
    CONSTRAINT FK_Meters_Customers FOREIGN KEY (CustomerId) REFERENCES data.Customers (Id),
    CONSTRAINT UQ_Meters_MeterNumber UNIQUE (MeterNumber),
    CONSTRAINT CK_Meters_MeterNumber CHECK (LEN(LTRIM(RTRIM(MeterNumber))) > 0),
    CONSTRAINT CK_Meters_Status CHECK (Status IN ('Active', 'Inactive'))   -- soft delete = Inactive
);
GO
CREATE INDEX IX_Meters_CustomerId ON data.Meters (CustomerId);
GO

CREATE TABLE data.MeterReadings (
    Id           uniqueidentifier NOT NULL
                 CONSTRAINT PK_MeterReadings PRIMARY KEY
                 CONSTRAINT DF_MeterReadings_Id DEFAULT NEWID(),
    MeterId      uniqueidentifier NOT NULL,
    ReadingDate  datetime2        NOT NULL,
    KwhReading   decimal(12, 2)   NOT NULL,   
    CONSTRAINT FK_MeterReadings_Meters FOREIGN KEY (MeterId) REFERENCES data.Meters (Id),
    -- one reading per meter per timestamp; DESC order also serves "latest reading" lookups
    CONSTRAINT UQ_MeterReadings_Meter_Date UNIQUE (MeterId, ReadingDate DESC),
    CONSTRAINT CK_MeterReadings_Kwh CHECK (KwhReading >= 0)
);
GO

CREATE TABLE data.Bills (
    Id           uniqueidentifier NOT NULL
                 CONSTRAINT PK_Bills PRIMARY KEY
                 CONSTRAINT DF_Bills_Id DEFAULT NEWID(),
    MeterId      uniqueidentifier NOT NULL,
    PeriodStart  date             NOT NULL,
    PeriodEnd    date             NOT NULL,
    KwhUsed      decimal(12, 2)   NOT NULL,   
    RatePerKwh   decimal(8, 4)    NOT NULL,   
    Amount       decimal(10, 2)   NOT NULL,   
    Status       nvarchar(20)     NOT NULL CONSTRAINT DF_Bills_Status DEFAULT 'Unpaid',
    DueDate      date             NOT NULL,
    PaidDate     date             NULL,
    CreatedAt    datetime2        NOT NULL CONSTRAINT DF_Bills_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Bills_Meters FOREIGN KEY (MeterId) REFERENCES data.Meters (Id),
    CONSTRAINT UQ_Bills_Meter_Period UNIQUE (MeterId, PeriodStart, PeriodEnd),
    CONSTRAINT CK_Bills_Period  CHECK (PeriodEnd > PeriodStart),
    CONSTRAINT CK_Bills_Status  CHECK (Status IN ('Unpaid', 'Paid')),
    CONSTRAINT CK_Bills_Amounts CHECK (KwhUsed >= 0 AND RatePerKwh >= 0 AND Amount >= 0),
    CONSTRAINT CK_Bills_PaidDate CHECK (
        (Status = 'Paid'   AND PaidDate IS NOT NULL) OR
        (Status = 'Unpaid' AND PaidDate IS NULL))
);
GO
CREATE INDEX IX_Bills_Meter_Status ON data.Bills (MeterId, Status);
GO

/* Tier 2 */
CREATE TABLE data.ServiceRequests (
    Id           uniqueidentifier NOT NULL
                 CONSTRAINT PK_ServiceRequests PRIMARY KEY
                 CONSTRAINT DF_ServiceRequests_Id DEFAULT NEWID(),
    CustomerId   uniqueidentifier NOT NULL,
    Type         nvarchar(20)     NOT NULL,
    Description  nvarchar(1000)   NOT NULL,
    Status       nvarchar(20)     NOT NULL CONSTRAINT DF_ServiceRequests_Status DEFAULT 'Open',
    CreatedAt    datetime2        NOT NULL CONSTRAINT DF_ServiceRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2        NOT NULL CONSTRAINT DF_ServiceRequests_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ServiceRequests_Customers FOREIGN KEY (CustomerId) REFERENCES data.Customers (Id),
    CONSTRAINT CK_ServiceRequests_Type CHECK (Type IN ('Outage', 'Billing', 'Meter Issue', 'Other')),
    CONSTRAINT CK_ServiceRequests_Status CHECK (Status IN ('Open', 'In Progress', 'Resolved')),
    CONSTRAINT CK_ServiceRequests_Description CHECK (LEN(LTRIM(RTRIM(Description))) > 0)
);
GO
CREATE INDEX IX_ServiceRequests_Customer ON data.ServiceRequests (CustomerId);
GO

/* Tier 3 (simulated data) */
CREATE TABLE data.OutageStatus (
    Id                uniqueidentifier NOT NULL
                      CONSTRAINT PK_OutageStatus PRIMARY KEY
                      CONSTRAINT DF_OutageStatus_Id DEFAULT NEWID(),
    Region            nvarchar(30)     NOT NULL,
    CustomersTracked  int              NOT NULL,
    CustomersOut      int              NOT NULL,
    UpdatedAt         datetime2        NOT NULL CONSTRAINT DF_OutageStatus_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_OutageStatus_Region UNIQUE (Region),
    CONSTRAINT CK_OutageStatus_Region CHECK (Region IN
        ('Bronx', 'Brooklyn', 'Manhattan', 'Queens', 'Staten Island', 'Westchester')),
    CONSTRAINT CK_OutageStatus_Counts CHECK (CustomersTracked >= 0 AND CustomersOut >= 0 AND CustomersOut <= CustomersTracked)
);
GO


/* =====================================================================
   TRIGGER: readings can never go backwards for a meter
   A CHECK constraint can't compare rows, so this is a trigger on the table.
   ===================================================================== */
CREATE TRIGGER data.trg_MeterReadings_NoDecrease
ON data.MeterReadings
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE(KwhReading) OR UPDATE(ReadingDate) OR UPDATE(MeterId))
        RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN data.MeterReadings AS o
          ON o.MeterId = i.MeterId
         AND o.Id <> i.Id
        WHERE (o.ReadingDate < i.ReadingDate AND o.KwhReading > i.KwhReading)   -- lower than an earlier reading
           OR (o.ReadingDate > i.ReadingDate AND o.KwhReading < i.KwhReading)   -- higher than a later reading
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'Meter reading cannot be lower than an earlier reading for the same meter.', 1;
    END
END;
GO


/* =====================================================================
   VIEWS (schema: api)
   One-table views: simple, so INSERT / UPDATE work straight through them.
   Every NOT NULL column without a default is included, so inserts work.
   ===================================================================== */

CREATE VIEW api.vw_Users AS
SELECT Id, Email, PasswordHash, Role, IsActive, CreatedAt
FROM data.Users;
GO

CREATE VIEW api.vw_Customers AS
SELECT Id, UserId, AccountNumber, FullName, Phone, ServiceAddress, Region
FROM data.Customers;
GO

CREATE VIEW api.vw_Meters AS
SELECT Id, CustomerId, MeterNumber, Status, InstalledDate
FROM data.Meters;
GO

CREATE VIEW api.vw_MeterReadings AS
SELECT Id, MeterId, ReadingDate, KwhReading
FROM data.MeterReadings;
GO

CREATE VIEW api.vw_Bills AS
SELECT Id, MeterId, PeriodStart, PeriodEnd, KwhUsed, RatePerKwh, Amount,
       Status, DueDate, PaidDate, CreatedAt
FROM data.Bills;
GO

CREATE VIEW api.vw_ServiceRequests AS
SELECT Id, CustomerId, Type, Description, Status, CreatedAt, UpdatedAt
FROM data.ServiceRequests;
GO

CREATE VIEW api.vw_OutageStatus AS
SELECT Id, Region, CustomersTracked, CustomersOut, UpdatedAt
FROM data.OutageStatus;
GO

/* Read-only join views: the backend only SELECTs from these, never writes. */

CREATE VIEW api.vw_MeterLatestReading AS
SELECT m.Id          AS MeterId,
       m.CustomerId  AS CustomerId,
       m.MeterNumber AS MeterNumber,
       r.Id          AS ReadingId,
       r.ReadingDate AS ReadingDate,
       r.KwhReading  AS KwhReading
FROM data.Meters AS m
OUTER APPLY (
    SELECT TOP (1) Id, ReadingDate, KwhReading
    FROM data.MeterReadings
    WHERE MeterId = m.Id
    ORDER BY ReadingDate DESC
) AS r;
GO

CREATE VIEW api.vw_CustomerBillSummary AS
SELECT b.Id           AS BillId,
       c.Id           AS CustomerId,
       c.AccountNumber AS AccountNumber,
       c.FullName     AS FullName,
       c.Region       AS Region,
       m.Id           AS MeterId,
       m.MeterNumber  AS MeterNumber,
       b.PeriodStart  AS PeriodStart,
       b.PeriodEnd    AS PeriodEnd,
       b.KwhUsed      AS KwhUsed,
       b.RatePerKwh   AS RatePerKwh,
       b.Amount       AS Amount,
       b.Status       AS Status,
       b.DueDate      AS DueDate,
       b.PaidDate     AS PaidDate
FROM data.Bills AS b
JOIN data.Meters AS m    ON m.Id = b.MeterId
JOIN data.Customers AS c ON c.Id = m.CustomerId;
GO