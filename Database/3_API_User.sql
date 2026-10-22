/* =====================================================================
   03_api_user.sql  -  the account the backend API connects with

   The API account can read and write through the api views only.
   It has no access to the data tables and cannot DELETE anything.
   ===================================================================== */

/* ---------- LOCAL SQL SERVER ---------- */
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'utilities_api')
    CREATE LOGIN utilities_api WITH PASSWORD = N'CHANGE_ME_Strong#Password1';
GO

USE UtilitiesPortal;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'utilities_api')
    CREATE USER utilities_api FOR LOGIN utilities_api;
GO

/* ---------- Permissions (same for local and Azure) ---------- */
GRANT SELECT, INSERT, UPDATE ON SCHEMA::api TO utilities_api;
DENY DELETE ON SCHEMA::api TO utilities_api;
GO
/* Nothing is granted on schema data. The views can still read the tables
   because both schemas are owned by dbo (ownership chaining). */

PRINT '03_api_user.sql finished.';
GO

/* ---------- AZURE SQL DATABASE (use this instead of the LOCAL block) ----------
   Azure SQL Database uses a user that lives inside the database. Connect to the
   UtilitiesPortal database, then run:

   CREATE USER utilities_api WITH PASSWORD = N'CHANGE_ME_Strong#Password1';
   GRANT SELECT, INSERT, UPDATE ON SCHEMA::api TO utilities_api;
   DENY DELETE ON SCHEMA::api TO utilities_api;
   ------------------------------------------------------------------------ */