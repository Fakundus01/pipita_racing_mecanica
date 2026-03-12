using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PipitaDesktop.Data;

public static class ProfileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string AppRootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PipitaGarageDesktop");

    public static string ProfilesDirectory => Path.Combine(AppRootDirectory, "profiles");
    public static string ProfilesStatePath => Path.Combine(AppRootDirectory, "profiles.json");
    public static string LegacyDatabasePath => Path.Combine(AppRootDirectory, "pipita-desktop.db");

    public static AppProfilesState EnsureInitializedState()
    {
        Directory.CreateDirectory(AppRootDirectory);
        Directory.CreateDirectory(ProfilesDirectory);

        var state = LoadState();
        if (state.Profiles.Count == 0)
        {
            var profile = CreateProfileInternal(state, "Principal", saveState: false);
            MigrateLegacyDatabaseIfPresent(profile);
            state.LastProfileId = profile.Id;
            SaveState(state);
            return state;
        }

        foreach (var profile in state.Profiles)
        {
            Directory.CreateDirectory(GetProfileDirectory(profile));
        }

        if (string.IsNullOrWhiteSpace(state.LastProfileId) || state.Profiles.All(x => x.Id != state.LastProfileId))
        {
            state.LastProfileId = state.Profiles[0].Id;
            SaveState(state);
        }

        return state;
    }

    public static AppProfilesState LoadState()
    {
        if (!File.Exists(ProfilesStatePath))
        {
            return ApplyBundledRemoteConfig(new AppProfilesState());
        }

        var json = File.ReadAllText(ProfilesStatePath);
        var state = JsonSerializer.Deserialize<AppProfilesState>(json, JsonOptions) ?? new AppProfilesState();
        return ApplyBundledRemoteConfig(state);
    }

    public static void SaveState(AppProfilesState state)
    {
        Directory.CreateDirectory(AppRootDirectory);
        Directory.CreateDirectory(ProfilesDirectory);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(ProfilesStatePath, json);
    }

    public static AppProfile CreateProfile(AppProfilesState state, string name, string? pin = null)
    {
        var profile = CreateProfileInternal(state, name, saveState: true);
        if (!string.IsNullOrWhiteSpace(pin))
        {
            SetPin(state, profile.Id, pin, saveState: true);
            profile = state.Profiles.First(x => x.Id == profile.Id);
        }

        return profile;
    }

    public static void RenameProfile(AppProfilesState state, string profileId, string newName)
    {
        var profile = GetProfile(state, profileId);
        profile.Name = newName.Trim();
        profile.UpdatedAt = DateTime.UtcNow;
        SaveState(state);
    }

    public static void DeleteProfile(AppProfilesState state, string profileId)
    {
        var profile = GetProfile(state, profileId);
        var profileDirectory = GetProfileDirectory(profile);
        state.Profiles.Remove(profile);

        if (Directory.Exists(profileDirectory))
        {
            Directory.Delete(profileDirectory, recursive: true);
        }

        if (state.Profiles.Count == 0)
        {
            var fallback = CreateProfileInternal(state, "Principal", saveState: false);
            state.LastProfileId = fallback.Id;
        }
        else if (state.LastProfileId == profileId)
        {
            state.LastProfileId = state.Profiles[0].Id;
        }

        SaveState(state);
    }

    public static void SetPin(AppProfilesState state, string profileId, string pin, bool saveState = true)
    {
        var profile = GetProfile(state, profileId);
        var hashed = ProfileSecurity.HashPin(pin.Trim());
        profile.PinHash = hashed.Hash;
        profile.PinSalt = hashed.Salt;
        profile.UpdatedAt = DateTime.UtcNow;

        if (saveState)
        {
            SaveState(state);
        }
    }

    public static void ClearPin(AppProfilesState state, string profileId)
    {
        var profile = GetProfile(state, profileId);
        profile.PinHash = null;
        profile.PinSalt = null;
        profile.UpdatedAt = DateTime.UtcNow;
        SaveState(state);
    }

    public static void SetLastProfile(AppProfilesState state, string profileId)
    {
        _ = GetProfile(state, profileId);
        state.LastProfileId = profileId;
        SaveState(state);
    }

    public static AppProfile? TryGetLastProfile(AppProfilesState state)
    {
        return state.Profiles.FirstOrDefault(x => x.Id == state.LastProfileId)
            ?? state.Profiles.FirstOrDefault();
    }

    public static AppProfile GetProfile(AppProfilesState state, string profileId)
    {
        return state.Profiles.FirstOrDefault(x => x.Id == profileId)
            ?? throw new InvalidOperationException("El perfil seleccionado ya no existe.");
    }

    public static string GetProfileDirectory(AppProfile profile)
    {
        return Path.Combine(ProfilesDirectory, profile.Slug);
    }

    public static string GetDatabasePath(AppProfile profile)
    {
        return Path.Combine(GetProfileDirectory(profile), "pipita-desktop.db");
    }

    public static string GetBackupDirectory(AppProfile profile)
    {
        return Path.Combine(GetProfileDirectory(profile), "backups");
    }

    private static AppProfile CreateProfileInternal(AppProfilesState state, string name, bool saveState)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new InvalidOperationException("El nombre del perfil es obligatorio.");
        }

        var slug = BuildUniqueSlug(state, trimmedName);
        var profile = new AppProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = trimmedName,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        state.Profiles.Add(profile);
        Directory.CreateDirectory(GetProfileDirectory(profile));

        if (saveState)
        {
            state.LastProfileId = profile.Id;
            SaveState(state);
        }

        return profile;
    }

    private static void MigrateLegacyDatabaseIfPresent(AppProfile profile)
    {
        if (!File.Exists(LegacyDatabasePath))
        {
            return;
        }

        var destination = GetDatabasePath(profile);
        Directory.CreateDirectory(GetProfileDirectory(profile));

        if (!File.Exists(destination))
        {
            File.Copy(LegacyDatabasePath, destination, overwrite: false);
        }
    }

    private static string BuildUniqueSlug(AppProfilesState state, string name)
    {
        var baseSlug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "perfil";
        }

        var candidate = baseSlug;
        var index = 2;
        while (state.Profiles.Any(x => string.Equals(x.Slug, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseSlug}-{index}";
            index++;
        }

        return candidate;
    }

    private static AppProfilesState ApplyBundledRemoteConfig(AppProfilesState state)
    {
        try
        {
            var bundledConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "remote-config.json");
            if (!File.Exists(bundledConfigPath))
            {
                return state;
            }

            var bundledJson = File.ReadAllText(bundledConfigPath);
            var bundledConfig = JsonSerializer.Deserialize<RemoteConfig>(bundledJson, JsonOptions);
            if (bundledConfig is null || string.IsNullOrWhiteSpace(bundledConfig.ApiBaseUrl))
            {
                return state;
            }

            if (string.IsNullOrWhiteSpace(state.ApiBaseUrl) || state.ApiBaseUrl == "http://localhost:5188")
            {
                state.ApiBaseUrl = bundledConfig.ApiBaseUrl.Trim();
            }
        }
        catch
        {
            // Si el archivo de configuracion no es valido, mantenemos la configuracion local existente.
        }

        return state;
    }
}

internal sealed class RemoteConfig
{
    public string? ApiBaseUrl { get; set; }
}

