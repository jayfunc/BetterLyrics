using System.Threading.Tasks;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IDatabaseMigrationService
{
    Task MigrateAllAsync();
}
