using System.IO;

namespace PipitaDesktop.Data;

public static class DatabasePathProvider
{
    public static string DatabaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PipitaGarageDesktop");

    public static string DatabasePath => Path.Combine(DatabaseDirectory, "pipita-desktop.db");
}
