/* =====================================================================
   04_verify.sql  -  checks that the database behaves as designed

   Run each numbered block on its own (select it, press Execute).
   Connected as an admin (sa / dbo), after scripts 01, 02 and 03.
   ===================================================================== */


/* ---------- 1. Views return data (expect rows) ---------- */
SELECT * FROM api.vw_CustomerBillSummary ORDER BY FullName, PeriodStart;
SELECT * FROM api.vw_MeterLatestReading ORDER BY MeterNumber;   -- expect Oct 1 reading for each meter
GO

/* ---------- 2. Trigger blocks a lower reading ----------
   EXPECTED: error 50001
   "Meter reading cannot be lower than an earlier reading for the same meter."
   Nothing is inserted. */
INSERT INTO api.vw_MeterReadings (Id, MeterId, ReadingDate, KwhReading)
SELECT NEWID(), Id, '2026-10-15', 100.00
FROM data.Meters WHERE MeterNumber = 'M-1001';
GO

/* ---------- 3. Trigger allows a valid reading ----------
   EXPECTED: 1 row shown with 13300.00, then everything is rolled back. */
BEGIN TRANSACTION;
INSERT INTO api.vw_MeterReadings (Id, MeterId, ReadingDate, KwhReading)
SELECT NEWID(), Id, '2026-10-15', 13300.00
FROM data.Meters WHERE MeterNumber = 'M-1001';
SELECT ReadingDate, KwhReading FROM api.vw_MeterReadings
WHERE MeterId = (SELECT Id FROM data.Meters WHERE MeterNumber = 'M-1001') AND ReadingDate = '2026-10-15';
ROLLBACK TRANSACTION;
GO

/* ---------- 4. Constraints block bad data ----------
   EXPECTED: each statement fails with a constraint error. */
INSERT INTO api.vw_Users (Id, Email, PasswordHash, Role)
VALUES (NEWID(), 'bad-role@utilitiesportal.test', 'x', 'Superuser');      -- CK_Users_Role
GO
INSERT INTO api.vw_Users (Id, Email, PasswordHash, Role)
VALUES (NEWID(), 'admin@utilitiesportal.test', 'x', 'Customer');          -- UQ_Users_Email (duplicate)
GO
UPDATE api.vw_Customers SET Phone = '12345' WHERE AccountNumber = 'CE-100001';   -- CK_Customers_Phone
GO

/* ---------- 5. API account can use views, not tables ----------
   Run these one at a time, in order. Always finish with REVERT. */
EXECUTE AS USER = 'utilities_api';
GO
SELECT TOP 3 Email, Role FROM api.vw_Users;       -- EXPECTED: 3 rows
GO
SELECT TOP 1 * FROM data.Users;                   -- EXPECTED: error 229, permission denied
GO
DELETE FROM api.vw_Meters WHERE 1 = 0;            -- EXPECTED: error 229, permission denied
GO
REVERT;
GO
SELECT USER_NAME() AS CurrentUser;                -- EXPECTED: dbo
GO
