using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public sealed class Migration_002_AddDaysColumn : IMigration
    {
        public int Version => 2;

        public void Up(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
ALTER TABLE Leaves
ADD COLUMN days INTEGER NOT NULL DEFAULT 0;
";
            cmd.ExecuteNonQuery();
        }
    }
}