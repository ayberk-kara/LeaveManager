using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_004_AddYearColumnToLeaves : IMigration
    {
        public int Version => 4;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
ALTER TABLE Leaves
ADD COLUMN year INTEGER NOT NULL DEFAULT 0;
";
            cmd.ExecuteNonQuery();
        }
    }
}