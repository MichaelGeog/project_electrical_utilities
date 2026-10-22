/* =====================================================================
   3_API_User_Azure.sql  -  the account the Web API connects with (Azure SQL Database)

   Connect to the UtilitiesPortal database as the server admin, after 1_Schema_Azure.sql.
   Replace CHANGE_ME_Strong#Password1 with your own strong password.
   NEVER commit the real password to git. Keep it in the App Service
   configuration (connection string) and nowhere else.

   The API account can read and write through the api views only.
   It has no access to the data tables and cannot DELETE anything.
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'utilities_api')
    CREATE USER utilities_api WITH PASSWORD = N'CHANGE_ME_Strong#Password1';
GO

GRANT SELECT, INSERT, UPDATE ON SCHEMA::api TO utilities_api;
DENY DELETE ON SCHEMA::api TO utilities_api;
GO
/* Nothing is granted on schema data. The views can still read the tables
   because both schemas are owned by dbo (ownership chaining). */

PRINT '3_API_User_Azure.sql finished.';
GO
