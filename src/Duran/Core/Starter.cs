using System.Security.Cryptography;
using System.Text;

namespace Duran.Core;

/// Config base de Hyprland que va dentro de duran: atajos `duran ipc …`, `duran start` al arrancar
/// y colores del shell activo (scheme/active.lua lee el theme de shell.lua).
/// `duran hypr init` la copia en ~/.config/hypr, salvo que esa config la gestiones tú.
public static class Starter
{
    const string HyprPrefix = "starter/hypr/";
    const string ScriptPrefix = "starter/scripts/";

    static string HyprDir => Path.Combine(Paths.ConfigHome, "hypr");
    static string ShaFile => Path.Combine(Paths.StateDir, "starter.sha");
    // Instalaciones anteriores hechas por velvet (scripts/hypr-starter.sh)
    static string LegacyShaFile => Path.Combine(Paths.StateHome, "velvet", "starter.sha");

    // Lo que es de este equipo y no de la config: se conserva al sustituir
    static readonly string[] KeepAlways = ["shell.lua", "hypridle.conf"];
    static readonly string[] KeepIfOurs = ["monitors.lua", "toggles.json", "hyprsunset.conf"];

    /// Archivos de la config base: (ruta relativa, nombre del recurso), en orden fijo
    static List<(string Rel, string Res)> Files() =>
        typeof(Starter).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(HyprPrefix))
            .Select(n => (n[HyprPrefix.Length..], n))
            .OrderBy(f => f.Item1, StringComparer.Ordinal)
            .ToList();

    static byte[] Read(string res)
    {
        using var s = typeof(Starter).Assembly.GetManifestResourceStream(res)!;
        using var m = new MemoryStream();
        s.CopyTo(m);
        return m.ToArray();
    }

    /// Huella de esta versión de la config (cambia si cambia cualquier archivo)
    static string Hash()
    {
        using var h = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (rel, res) in Files())
        {
            h.AppendData(Encoding.UTF8.GetBytes(rel + "\0"));
            h.AppendData(Read(res));
        }
        return Convert.ToHexString(h.GetHashAndReset())[..16].ToLowerInvariant();
    }

    static string? ReadSha(string file) =>
        File.Exists(file) && File.ReadAllText(file).Trim() is { Length: > 0 } s ? s : null;

    static bool IsLink(string path) => new FileInfo(path).LinkTarget != null;

    /// ¿La config la gestionas tú? (dotfiles con stow o git, o DURAN_KEEP_HYPR=1)
    static bool UserManaged()
    {
        var hypr = HyprDir;
        return new DirectoryInfo(hypr).LinkTarget != null
            || IsLink(Path.Combine(hypr, "hyprland.lua"))
            || IsLink(Path.Combine(hypr, "hyprland.conf"))
            || Directory.Exists(Path.Combine(hypr, ".git"))
            || Environment.GetEnvironmentVariable("DURAN_KEEP_HYPR") is { Length: > 0 }
            || Environment.GetEnvironmentVariable("VELVET_KEEP_HYPR") is { Length: > 0 };
    }

    public static int Init(bool yes)
    {
        var hypr = HyprDir;
        if (UserManaged())
        {
            Ui.Step("Hyprland: config propia (dotfiles o DURAN_KEEP_HYPR) — no se toca");
            return 0;
        }
        if (Files().Count == 0)
        {
            Ui.Error("este duran no lleva la config base de Hyprland");
            return 1;
        }

        var hash = Hash();
        var existing = File.Exists(Path.Combine(hypr, "hyprland.lua")) || File.Exists(Path.Combine(hypr, "hyprland.conf"));
        var previous = ReadSha(ShaFile) ?? ReadSha(LegacyShaFile);
        var wasOurs = previous != null;

        if (existing && previous == hash)
        {
            Ui.Step("Hyprland: la config base ya está al día");
            return 0;
        }

        // Copia de seguridad de lo que hubiera
        string? backup = null;
        if (existing)
        {
            backup = $"{hypr}.antes-de-duran-{DateTime.Now:yyyyMMdd-HHmmss}";
            Ui.Warn(wasOurs
                ? "hay una config base de Hyprland nueva y va a SUSTITUIR la tuya (~/.config/hypr)."
                : "duran va a SUSTITUIR tu config de Hyprland (~/.config/hypr) por la suya.");
            Ui.Warn($"La actual se guarda en {Paths.Pretty(backup)}.");
            if (!yes && !Console.IsInputRedirected)
            {
                Console.Write("    ¿Seguir? [S/n] ");
                var ans = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (ans is "n" or "no")
                {
                    Ui.Step("Hyprland: no se toca");
                    return 0;
                }
            }
            if (Exec.Run($"cp -a '{hypr}' '{backup}'") != 0)
            {
                Ui.Error("no se pudo hacer la copia de seguridad: no toco nada");
                return 1;
            }
            Exec.Run($"find '{hypr}' -mindepth 1 -maxdepth 1 -exec rm -rf {{}} +");
        }
        Directory.CreateDirectory(hypr);

        // Config base
        foreach (var (rel, res) in Files())
        {
            var dest = Path.Combine(hypr, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllBytes(dest, Read(res));
        }

        // Lo de este equipo vuelve a su sitio
        if (backup != null)
        {
            var keep = wasOurs ? KeepAlways.Concat(KeepIfOurs) : KeepAlways;
            foreach (var f in keep)
            {
                var src = Path.Combine(backup, f);
                if (!File.Exists(src) && !IsLink(src))
                    continue;
                var dst = Path.Combine(hypr, f);
                File.Delete(dst);
                Exec.Run($"cp -a '{src}' '{dst}'");
            }
        }

        Ui.Step("apps de tu sistema → variables.lua");
        RunScript("detect-apps.sh", Path.Combine(hypr, "variables.lua"));
        RunScript("detect-kde.sh", hypr);

        Directory.CreateDirectory(Paths.StateDir);
        File.WriteAllText(ShaFile, hash + "\n");
        Ui.Step("Hyprland: config base de duran instalada");
        Hypr.Reload();
        return 0;
    }

    /// Ejecuta uno de los scripts que van dentro de duran
    static void RunScript(string name, string arg)
    {
        var res = ScriptPrefix + name;
        if (typeof(Starter).Assembly.GetManifestResourceInfo(res) == null)
            return;
        var tmp = Path.Combine(Path.GetTempPath(), $"duran-{Environment.ProcessId}-{name}");
        File.WriteAllBytes(tmp, Read(res));
        Exec.Run($"bash '{tmp}' '{arg}'");
        File.Delete(tmp);
    }
}