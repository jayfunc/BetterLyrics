namespace BetterLyrics.Core.Interfaces.Services;

public interface IDatabaseMigrationService
{
    Task MigrateAllAsync();
}
