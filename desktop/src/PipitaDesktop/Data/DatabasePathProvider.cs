using System.IO;

namespace PipitaDesktop.Data;

public static class DatabasePathProvider
{
    public static string DatabaseDirectory => ActiveProfileContext.CurrentProfile is not null
        ? ProfileManager.GetProfileDirectory(ActiveProfileContext.CurrentProfile)
        : ProfileManager.AppRootDirectory;

    public static string DatabasePath => ActiveProfileContext.CurrentProfile is not null
        ? ProfileManager.GetDatabasePath(ActiveProfileContext.CurrentProfile)
        : ProfileManager.LegacyDatabasePath;
}
