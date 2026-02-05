using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_004_FixLeavesEmployeeId : IMigration
    {
        public int Version => 4;

        public void Up(SqliteConnection connection)
        {
            // If Leaves table doesn't exist yet, create it in the correct shape.
            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Leaves (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId INTEGER NOT NULL,
    StartDate  TEXT    NOT NULL,
    EndDate    TEXT    NOT NULL,
    Type       TEXT    NOT NULL,
    CreatedAt  TEXT    NOT NULL
);
";
                createCmd.ExecuteNonQuery();
            }

            // EmployeeId column exists
            var hasEmployeeId = false;

            using (var infoCmd = connection.CreateCommand())
            {
                infoCmd.CommandText = @"PRAGMA table_info(Leaves);";
                using var reader = infoCmd.ExecuteReader();
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    if (string.Equals(colName, "EmployeeId", System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasEmployeeId = true;
                        break;
                    }
                }
            }

            if (!hasEmployeeId)
            {
              
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = @"
ALTER TABLE Leaves ADD COLUMN EmployeeId INTEGER NOT NULL DEFAULT 0;
";
                alterCmd.ExecuteNonQuery();
            }

            
            using (var idxCmd = connection.CreateCommand())
            {
                idxCmd.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_Leaves_EmployeeId ON Leaves(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_Leaves_StartDate  ON Leaves(StartDate);
";
                idxCmd.ExecuteNonQuery();
            }
        }
    }
}
