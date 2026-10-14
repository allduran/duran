using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran install <shell | url-git | usuario/repo>
///   · nombre: un shell local (ya en ~/.config/quickshell, p. ej. desde dotfiles)
///   · url o usuario/repo: lo clona en ~/.local/share/duran/shells/<nombre> y lo enlaza en ~/.config/quickshell
/// Después instala los paquetes de [packages] y ejecuta su hook de instalación.
public static class InstallCommand
{
    public static Command Create()
    {
        var targetArg = new Argument<string>("shell") { Description = "Nombre local, URL de git o usuario/repo de GitHub" };
        var noPkgs = new Option<bool>("--no-packages") { Description = "No instalar paquetes" };
        var noHook = new Option<bool>("--no-hook") { Description = "No ejecutar hooks/install.sh" };
        var switchTo = new Option<bool>("--switch", "-s") { Description = "Cambiar a este shell al terminar" };

        var cmd = new Command("install", "Instala un shell (local o desde git)");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(noPkgs);
        cmd.Options.Add(noHook);
        cmd.Options.Add(switchTo);
        cmd.SetAction(p => Run(p.GetValue(targetArg)!, !p.GetValue(noPkgs), !p.GetValue(noHook), p.GetValue(switchTo)));
        return cmd;
    }

    static bool IsGit(string t) => t.Contains("://") || t.StartsWith("git@") || t.Count(c => c == '/') == 1;

    static string GitUrl(string t) => t.Contains("://") || t.StartsWith("git@") ? t : $"https://github.com/{t}.git";

    static int Run(string target, bool packages, bool hook, bool switchAfter)
    {
        var shell = IsGit(target) ? Clone(GitUrl(target)) : Catalog.Find(target);
        if (shell == null)
        {
            if (!IsGit(target))
                Ui.Error($"No encuentro el shell '{target}'. Para instalar desde git usa una URL o usuario/repo.");
            return 1;
        }

        Console.WriteLine($"{Ui.Bold("duran install")} {Ui.Bold(shell.Name)} {Ui.Dim("v" + shell.Version)}");

        // Paquetes
        if (packages)
        {
            var all = shell.RepoPackages.Concat(shell.AurPackages).Distinct().ToList();
            var missing = Missing(all);
            if (missing.Count == 0)
                Ui.Step("paquetes: todo instalado");
            else
            {
                Ui.Step($"paquetes que faltan: {string.Join(" ", missing)}");
                var pkgs = string.Join(" ", missing);
                int code;
                if (Exec.Has("paru"))
                    code = Exec.Run($"paru -S --needed {pkgs}");
                else
                {
                    var aur = missing.Intersect(shell.AurPackages).ToList();
                    if (aur.Count > 0)
                        Ui.Warn($"sin paru no puedo instalar del AUR: {string.Join(" ", aur)}");
                    var repo = missing.Except(aur).ToList();
                    code = repo.Count == 0 ? 0 : Exec.Run($"sudo pacman -S --needed {string.Join(" ", repo)}");
                }
                if (code != 0)
                {
                    Ui.Error("la instalación de paquetes falló");
                    return 1;
                }
            }
        }

        // Hook
        if (hook && shell.InstallHook.Length > 0)
        {
            var path = Path.Combine(shell.Root, shell.InstallHook);
            if (File.Exists(path))
            {
                Ui.Step($"hook: {shell.InstallHook}");
                if (Exec.Run($"bash '{path}'", shell.Root) != 0)
                {
                    Ui.Error("el hook de instalación falló");
                    return 1;
                }
            }
            else
                Ui.Warn($"no existe {shell.InstallHook}");
        }

        Console.WriteLine($"{Ui.Green("✓")} {shell.Name} instalado");

        if (switchAfter)
            return SwitchCommand.Run(shell.Name);
        Console.WriteLine(Ui.Dim($"  para usarlo: duran switch {shell.Name}"));
        return 0;
    }

    /// Paquetes que no están instalados (pacman -T lista los que faltan)
    static List<string> Missing(List<string> pkgs)
    {
        if (pkgs.Count == 0)
            return [];
        var (_, output) = Exec.Capture("pacman -T " + string.Join(" ", pkgs.Select(p => $"'{p}'")));
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// Clona el repo y lo enlaza en ~/.config/quickshell/<nombre>
    static ShellManifest? Clone(string url)
    {
        var repoName = Path.GetFileNameWithoutExtension(url.TrimEnd('/'));
        var dest = Path.Combine(Paths.RemoteShellsDir, repoName);
        Directory.CreateDirectory(Paths.RemoteShellsDir);

        if (Directory.Exists(dest))
            Ui.Step($"ya clonado en {Paths.Pretty(dest)}");
        else
        {
            Ui.Step($"clonando {url}");
            if (Exec.Run($"git clone --quiet '{url}' '{dest}'") != 0)
            {
                Ui.Error("git clone falló");
                return null;
            }
        }

        var toml = Path.Combine(dest, "shell.toml");
        if (!File.Exists(toml))
        {
            Ui.Error($"{url} no tiene shell.toml en la raíz: no es un shell para duran");
            Directory.Delete(dest, recursive: true);
            return null;
        }
        var m = ShellManifest.Load(toml, ShellSource.Remote);

        // Enlace para que Quickshell lo encuentre con `qs -c <nombre>`
        var link = Path.Combine(Paths.QuickshellDir, m.Name);
        Directory.CreateDirectory(Paths.QuickshellDir);
        if (!Directory.Exists(link) && !File.Exists(link))
        {
            Directory.CreateSymbolicLink(link, dest);
            Ui.Step($"enlazado en {Paths.Pretty(link)}");
        }
        else if (!Catalog.IsRemoteLink(link))
        {
            Ui.Error($"{Paths.Pretty(link)} ya existe y no es de duran: no lo toco");
            return null;
        }
        return m;
    }
}