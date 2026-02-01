using System.Windows;
using LeaveManager.Data;
using LeaveManager.Data.Storage;

namespace LeaveManager.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DbInitializer.EnsureDatabaseReady();
            MigrationRunner.RunMigrations();
        }
    }
}
