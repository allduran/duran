namespace Duran.Core;

/// Estado de duran en ~/.local/state/duran/. De momento solo el shell activo (texto plano).
public static class State
{
    static string ActiveFile => Path.Combine(Paths.StateDir, "active");

    /// Shell elegido con `duran switch` (aunque ahora mismo no esté corriendo)
    public static string? Active
    {
        get => File.Exists(ActiveFile) && File.ReadAllText(ActiveFile).Trim() is { Length: > 0 } s ? s : null;
        set
        {
            Directory.CreateDirectory(Paths.StateDir);
            if (value is null)
                File.Delete(ActiveFile);
            else
                File.WriteAllText(ActiveFile, value + "\n");
        }
    }
}