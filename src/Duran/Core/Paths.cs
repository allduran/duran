namespace Duran.Core;

/// Rutas XDG que usa duran.
public static class Paths
{
    public static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    static string Xdg(string variable, string fallback) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } v ? v : Path.Combine(Home, fallback);

    public static string ConfigHome => Xdg("XDG_CONFIG_HOME", ".config");
    public static string StateHome => Xdg("XDG_STATE_HOME", ".local/state");
    public static string DataHome => Xdg("XDG_DATA_HOME", ".local/share");

    /// Shells locales: ~/.config/quickshell/<nombre>/shell.toml (enlazados con stow desde dotfiles)
    public static string QuickshellDir => Path.Combine(ConfigHome, "quickshell");

    /// Shells clonados desde git: ~/.local/share/duran/shells/<nombre>
    public static string RemoteShellsDir => Path.Combine(DataHome, "duran", "shells");

    /// Estado de duran (shell activo, instalados…)
    public static string StateDir => Path.Combine(StateHome, "duran");

    /// "~/algo" → "/home/usuario/algo"
    public static string Expand(string path) =>
        path == "~" ? Home : path.StartsWith("~/") ? Path.Combine(Home, path[2..]) : path;

    /// "/home/usuario/algo" → "~/algo" (para mostrar)
    public static string Pretty(string path) =>
        path.StartsWith(Home) ? "~" + path[Home.Length..] : path;
}