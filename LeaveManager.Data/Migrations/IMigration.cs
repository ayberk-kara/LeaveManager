using Microsoft.Data.Sqlite;

namespace LeaveManager.Data.Migrations
{
    public interface IMigration
    {
        int Version { get; }
        void Up(SqliteConnection connection);
    }
}
