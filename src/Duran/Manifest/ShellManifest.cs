using Tomlyn;
using Tomlyn.Model;
using System.Diagnostics.CodeAnalysis;

namespace Duran.Manifest;

public enum ShellSource { Local, Remote }

/// Lo que declara el shell.toml de un shell.
/// Se lee como tabla sin tipos (TomlTable): no necesita reflexión, así que va bien con AOT.
public sealed class ShellManifest
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string Version { get; init; } = "";
    public string Engine { get; init; } = "quickshell";
    public string ConfigDir { get; init; } = "";
    public string Start { get; init; } = "";
    public string Stop { get; init; } = "";

    public List<string> RepoPackages { get; init; } = [];
    public List<string> AurPackages { get; init; } = [];
    public Dictionary<string, string> Ipc { get; init; } = [];
    public List<string> ConflictShells { get; init; } = [];
    public string InstallHook { get; init; } = "";
    public string UninstallHook { get; init; } = "";

    /// Carpeta donde está el shell.toml
    public required string Root { get; init; }
    public required ShellSource Source { get; init; }

    public static ShellManifest Load(string tomlPath, ShellSource source)
    {
                var table = ParseTable(File.ReadAllText(tomlPath))
                    ?? throw new InvalidDataException("shell.toml vacío");
        var root = Path.GetDirectoryName(Path.GetFullPath(tomlPath))!;

        var shell = Sub(table, "shell");
        var packages = Sub(table, "packages");
        var conflicts = Sub(table, "conflicts");
        var hooks = Sub(table, "hooks");

        return new ShellManifest
        {
            Name = Str(shell, "name") is { Length: > 0 } n ? n : Path.GetFileName(root),
            Description = Str(shell, "description"),
            Version = Str(shell, "version"),
            Engine = Str(shell, "engine", "quickshell"),
            ConfigDir = Str(shell, "config_dir"),
            Start = Str(shell, "start"),
            Stop = Str(shell, "stop"),
            RepoPackages = Strs(packages, "repo"),
            AurPackages = Strs(packages, "aur"),
            Ipc = StrMap(Sub(table, "ipc")),
            ConflictShells = Strs(conflicts, "shells"),
            InstallHook = Str(hooks, "install"),
            UninstallHook = Str(hooks, "uninstall"),
            Root = root,
            Source = source,
        };
    }

        // TomlTable es el modelo sin tipos de Tomlyn: no se recorre por reflexión,
    // así que funciona con AOT aunque el analizador avise (probado con velvet).
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TomlTable no necesita reflexión")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TomlTable no necesita reflexión")]
    static TomlTable? ParseTable(string toml) => TomlSerializer.Deserialize<TomlTable>(toml);

    // ── Lectura tolerante: si falta una clave, valor por defecto ──
    static TomlTable? Sub(TomlTable? t, string key) =>
        t != null && t.TryGetValue(key, out var v) ? v as TomlTable : null;

    static string Str(TomlTable? t, string key, string fallback = "") =>
        t != null && t.TryGetValue(key, out var v) && v is string s ? s : fallback;

    static List<string> Strs(TomlTable? t, string key) =>
        t != null && t.TryGetValue(key, out var v) && v is TomlArray a ? a.OfType<string>().ToList() : [];

    static Dictionary<string, string> StrMap(TomlTable? t) =>
        t == null ? [] : t.Where(kv => kv.Value is string).ToDictionary(kv => kv.Key, kv => (string)kv.Value!);
}