/* =====================================================================
   02_seed.sql  -  sample data for development

   **** this script DELETES ALL DATA in the UtilitiesPortal tables first,
   then inserts fresh sample data. Safe to rerun; never run it on real data*****

   Test logins:
     admin@utilitiesportal.test           Admin#2026Test
     maria.santos@utilitiesportal.test    Customer#2026Test
     james.carter@utilitiesportal.test    Customer#2026Test
     priya.patel@utilitiesportal.test     Customer#2026Test

   Sample data: 1 admin, 3 customers, 4 meters (Priya has two), 4 readings per
   meter (Jul 1 to Oct 1), 6 bills. The Sep 1 to Oct 1 period is left unbilled
   on purpose so you can test bill generation. All names, addresses and phone
   numbers are made up.
   ===================================================================== */

USE UtilitiesPortal; 
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- Clear existing data (children first) ---------- */
DELETE FROM data.Bills;
DELETE FROM data.MeterReadings;
DELETE FROM data.ServiceRequests;
DELETE FROM data.Meters;
DELETE FROM data.Customers;
DELETE FROM data.Users;
DELETE FROM data.OutageStatus;

/* ---------- IDs and password hashes (BCrypt, cost 12) ---------- */
DECLARE @AdminHash nvarchar(200) = N'$2b$12$M3f7YW/fJtdOgf/DxM9oaOmNdb56xOX7f68zy9n7s.UnALU6Ok6li';
DECLARE @CustHash  nvarchar(200) = N'$2b$12$2kteg55DXpB58p/YKKlPSeu6vIlOZtP3prHSIOc4nHUlihj//jZqq';

DECLARE @UserAdmin uniqueidentifier = NEWID();
DECLARE @UserMaria uniqueidentifier = NEWID();
DECLARE @UserJames uniqueidentifier = NEWID();
DECLARE @UserPriya uniqueidentifier = NEWID();

DECLARE @CustMaria uniqueidentifier = NEWID();
DECLARE @CustJames uniqueidentifier = NEWID();
DECLARE @CustPriya uniqueidentifier = NEWID();

DECLARE @Meter1 uniqueidentifier = NEWID();   -- Maria
DECLARE @Meter2 uniqueidentifier = NEWID();   -- James
DECLARE @Meter3 uniqueidentifier = NEWID();   -- Priya, main house
DECLARE @Meter4 uniqueidentifier = NEWID();   -- Priya, garage / second meter

/* ---------- Users ---------- */
INSERT INTO data.Users (Id, Email, PasswordHash, Role) VALUES
    (@UserAdmin, 'admin@utilitiesportal.test',        @AdminHash, 'Admin'),
    (@UserMaria, 'maria.santos@utilitiesportal.test', @CustHash,  'Customer'),
    (@UserJames, 'james.carter@utilitiesportal.test', @CustHash,  'Customer'),
    (@UserPriya, 'priya.patel@utilitiesportal.test',  @CustHash,  'Customer');

/* ---------- Customers ---------- */
INSERT INTO data.Customers (Id, UserId, AccountNumber, FullName, Phone, ServiceAddress, Region) VALUES
    (@CustMaria, @UserMaria, 'CE-100001', 'Maria Santos', '7185550101', '123 Example Ave, Queens, NY 11101',      'Queens'),
    (@CustJames, @UserJames, 'CE-100002', 'James Carter', '2125550102', '88 Sample St, New York, NY 10001',       'Manhattan'),
    (@CustPriya, @UserPriya, 'CE-100003', 'Priya Patel',  '9145550103', '12 Demo Rd, White Plains, NY 10601',    'Westchester');

/* ---------- Meters ---------- */
INSERT INTO data.Meters (Id, CustomerId, MeterNumber, Status, InstalledDate) VALUES
    (@Meter1, @CustMaria, 'M-1001', 'Active', '2022-03-15'),
    (@Meter2, @CustJames, 'M-1002', 'Active', '2021-11-02'),
    (@Meter3, @CustPriya, 'M-1003', 'Active', '2020-06-20'),
    (@Meter4, @CustPriya, 'M-1004', 'Active', '2023-01-10');

/* ---------- Meter readings (cumulative kWh, one per month) ---------- */
INSERT INTO data.MeterReadings (Id, MeterId, ReadingDate, KwhReading) VALUES
    (NEWID(), @Meter1, '2026-07-01', 12000.00),
    (NEWID(), @Meter1, '2026-08-01', 12410.00),
    (NEWID(), @Meter1, '2026-09-01', 12835.00),
    (NEWID(), @Meter1, '2026-10-01', 13270.00),

    (NEWID(), @Meter2, '2026-07-01',  8000.00),
    (NEWID(), @Meter2, '2026-08-01',  8325.00),
    (NEWID(), @Meter2, '2026-09-01',  8680.00),
    (NEWID(), @Meter2, '2026-10-01',  9010.00),

    (NEWID(), @Meter3, '2026-07-01', 20000.00),
    (NEWID(), @Meter3, '2026-08-01', 20780.00),
    (NEWID(), @Meter3, '2026-09-01', 21590.00),
    (NEWID(), @Meter3, '2026-10-01', 22350.00),

    (NEWID(), @Meter4, '2026-07-01',   500.00),
    (NEWID(), @Meter4, '2026-08-01',   640.00),
    (NEWID(), @Meter4, '2026-09-01',   790.00),
    (NEWID(), @Meter4, '2026-10-01',   925.00);

/* ---------- Bills (flat rate 0.2500 per kWh; Amount = KwhUsed x rate) ---------- */
INSERT INTO data.Bills (Id, MeterId, PeriodStart, PeriodEnd, KwhUsed, RatePerKwh, Amount, Status, DueDate, PaidDate) VALUES
    (NEWID(), @Meter1, '2026-07-01', '2026-08-01', 410.00, 0.2500, 102.50, 'Paid',   '2026-08-22', '2026-08-15'),
    (NEWID(), @Meter1, '2026-08-01', '2026-09-01', 425.00, 0.2500, 106.25, 'Unpaid', '2026-09-22', NULL),
    (NEWID(), @Meter2, '2026-07-01', '2026-08-01', 325.00, 0.2500,  81.25, 'Paid',   '2026-08-22', '2026-08-18'),
    (NEWID(), @Meter2, '2026-08-01', '2026-09-01', 355.00, 0.2500,  88.75, 'Unpaid', '2026-09-22', NULL),
    (NEWID(), @Meter3, '2026-07-01', '2026-08-01', 780.00, 0.2500, 195.00, 'Paid',   '2026-08-22', '2026-08-20'),
    (NEWID(), @Meter4, '2026-07-01', '2026-08-01', 140.00, 0.2500,  35.00, 'Unpaid', '2026-08-22', NULL);

/* ---------- Service requests (Tier 2) ---------- */
INSERT INTO data.ServiceRequests (Id, CustomerId, Type, Description, Status) VALUES
    (NEWID(), @CustMaria, 'Billing',     'My August bill looks higher than usual. Can someone review it?', 'Open'),
    (NEWID(), @CustJames, 'Meter Issue', 'The meter display is flickering and hard to read.',               'In Progress');

/* ---------- Outage status (Tier 3, SIMULATED sample data) ---------- */
INSERT INTO data.OutageStatus (Id, Region, CustomersTracked, CustomersOut) VALUES
    (NEWID(), 'Bronx',         481928,  0),
    (NEWID(), 'Brooklyn',      965878, 19),
    (NEWID(), 'Manhattan',     758956,  9),
    (NEWID(), 'Queens',        868226, 87),
    (NEWID(), 'Staten Island', 183590,  1),
    (NEWID(), 'Westchester',   368028,  1);

COMMIT TRANSACTION;

/* ---------- Quick check: row counts read through the api views ---------- */
SELECT 'vw_Users'           AS ViewName, COUNT(*) AS RowCnt FROM api.vw_Users
UNION ALL SELECT 'vw_Customers',       COUNT(*) FROM api.vw_Customers
UNION ALL SELECT 'vw_Meters',          COUNT(*) FROM api.vw_Meters
UNION ALL SELECT 'vw_MeterReadings',   COUNT(*) FROM api.vw_MeterReadings
UNION ALL SELECT 'vw_Bills',           COUNT(*) FROM api.vw_Bills
UNION ALL SELECT 'vw_ServiceRequests', COUNT(*) FROM api.vw_ServiceRequests
UNION ALL SELECT 'vw_OutageStatus',    COUNT(*) FROM api.vw_OutageStatus;
GO