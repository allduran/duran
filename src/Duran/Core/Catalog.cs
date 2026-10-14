using Duran.Manifest;

namespace Duran.Core;

/// Encuentra los shells (locales y clonados) y sabe cuál está corriendo.
public static class Catalog
{
    /// Todos los shells con shell.toml. Si un nombre está en los dos sitios, gana el local.
    public static List<ShellManifest> Discover(List<string>? errors = null)
    {
        var found = new Dictionary<string, ShellManifest>(StringComparer.OrdinalIgnoreCase);

        void Scan(string dir, ShellSource source)
        {
            if (!Directory.Exists(dir))
                return;
            foreach (var shellDir in Directory.EnumerateDirectories(dir).Order())
            {
                var toml = Path.Combine(shellDir, "shell.toml");
                if (!File.Exists(toml))
                    continue;
                try
                {
                    var m = ShellManifest.Load(toml, source);
                    found.TryAdd(m.Name, m);
                }
                catch (Exception e)
                {
                    errors?.Add($"{Paths.Pretty(toml)}: {e.Message}");
                }
            }
        }

        Scan(Paths.QuickshellDir, ShellSource.Local);
        Scan(Paths.RemoteShellsDir, ShellSource.Remote);
        return found.Values.OrderBy(m => m.Name).ToList();
    }

    public static ShellManifest? Find(string name) =>
        Discover().FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// ¿Hay un proceso "qs/quickshell … -c <nombre>"? Lee /proc directamente (sin pgrep).
    public static bool IsRunning(string name)
    {
        foreach (var proc in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(proc), out _))
                continue;
            string[] argv;
            try
            {
                argv = File.ReadAllText(Path.Combine(proc, "cmdline")).Split('\0', StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                continue;   // el proceso terminó o no es nuestro
            }
            if (argv.Length < 3)
                continue;
            var exe = Path.GetFileName(argv[0]);
            if (exe != "qs" && exe != "quickshell")
                continue;
            for (var i = 1; i < argv.Length - 1; i++)
                if ((argv[i] == "-c" || argv[i] == "--config") && argv[i + 1] == name)
                    return true;
        }
        return false;
    }
}