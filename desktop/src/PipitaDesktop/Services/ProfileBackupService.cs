using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PipitaDesktop.Data;

namespace PipitaDesktop.Services;

public static class ProfileBackupService
{
    public static string BuildDefaultBackupName(AppProfile profile)
    {
        var safeSlug = string.IsNullOrWhiteSpace(profile.Slug) ? "perfil" : profile.Slug;
        return $"{safeSlug}-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
    }

    public static void CreateBackup(AppProfile profile, string destinationZipPath)
    {
        var sourceDb = ProfileManager.GetDatabasePath(profile);
        if (!File.Exists(sourceDb))
        {
            throw new FileNotFoundException("No existe la base de datos del perfil para respaldar.", sourceDb);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationZipPath)!);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "PipitaProfileBackup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            File.Copy(sourceDb, Path.Combine(tempDirectory, "pipita-desktop.db"), overwrite: true);

            var metadata = new
            {
                profile.Id,
                profile.Name,
                profile.Slug,
                profile.RemoteProfileId,
                ExportedAt = DateTime.UtcNow,
            };

            File.WriteAllText(
                Path.Combine(tempDirectory, "profile-metadata.json"),
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(tempDirectory, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    public static void RestoreBackup(AppProfile profile, string backupZipPath)
    {
        if (!File.Exists(backupZipPath))
        {
            throw new FileNotFoundException("No se encontro el backup seleccionado.", backupZipPath);
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "PipitaProfileRestore", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ZipFile.ExtractToDirectory(backupZipPath, tempDirectory, overwriteFiles: true);
            var extractedDb = Path.Combine(tempDirectory, "pipita-desktop.db");
            if (!File.Exists(extractedDb))
            {
                throw new InvalidOperationException("El backup no contiene una base valida.");
            }

            var targetDb = ProfileManager.GetDatabasePath(profile);
            Directory.CreateDirectory(Path.GetDirectoryName(targetDb)!);
            File.Copy(extractedDb, targetDb, overwrite: true);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
