using System.Windows;
using LeaveManager.Data;
using LeaveManager.Data.Storage;
using Microsoft.Data.Sqlite;

namespace LeaveManager.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create and open sqlite connection
            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = DbPaths.DatabasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString()
            );

            connection.Open();

            // run all pending migrations
            MigrationRunner.Run(connection);
        }
    }
}