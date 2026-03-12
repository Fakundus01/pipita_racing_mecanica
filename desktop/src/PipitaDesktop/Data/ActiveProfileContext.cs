namespace PipitaDesktop.Data;

public static class ActiveProfileContext
{
    public static AppProfile? CurrentProfile { get; private set; }

    public static bool HasActiveProfile => CurrentProfile is not null;

    public static void SetCurrentProfile(AppProfile profile)
    {
        CurrentProfile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public static void Clear()
    {
        CurrentProfile = null;
    }
}
