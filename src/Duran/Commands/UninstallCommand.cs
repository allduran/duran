using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran uninstall <shell> — lo para, ejecuta su hooks/uninstall.sh y, si es remoto, borra el clon.
/// Los paquetes no se tocan (pueden usarlos otros shells). Los shells locales (dotfiles) no se borran.
public static class UninstallCommand
{
    public static Command Create()
    {
        var shellArg = new Argument<string>("shell") { Description = "Shell a desinstalar" };
        var noHook = new Option<bool>("--no-hook") { Description = "No ejecutar hooks/uninstall.sh" };
        var yes = new Option<bool>("--yes", "-y") { Description = "No preguntar" };

        var cmd = new Command("uninstall", "Desinstala un shell");
        cmd.Arguments.Add(shellArg);
        cmd.Options.Add(noHook);
        cmd.Options.Add(yes);
        cmd.SetAction(p => Run(p.GetValue(shellArg)!, !p.GetValue(noHook), p.GetValue(yes)));
        return cmd;
    }

    static int Run(string name, bool hook, bool yes)
    {
        var s = Catalog.Find(name);
        if (s == null)
        {
            Ui.Error($"No encuentro el shell '{name}'.");
            return 1;
        }

        var remote = s.Source == ShellSource.Remote;
        Console.WriteLine($"{Ui.Bold("duran uninstall")} {Ui.Bold(s.Name)}");
        Console.WriteLine(remote
            ? Ui.Dim($"  se borrará el clon {Paths.Pretty(s.Root)}")
            : Ui.Dim("  es local: se para y se ejecuta su hook, pero sus archivos (dotfiles) se quedan"));

        if (!yes)
        {
            Console.Write("¿Seguir? [s/N] ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer is not ("s" or "si" or "sí" or "y" or "yes"))
            {
                Console.WriteLine("cancelado");
                return 1;
            }
        }

        if (Catalog.IsRunning(s.Name))
        {
            Ui.Step($"parando {s.Name}");
            Exec.Stop(s);
        }
        if (string.Equals(State.Active, s.Name, StringComparison.OrdinalIgnoreCase))
            State.Active = null;

        if (hook && s.UninstallHook.Length > 0)
        {
            var path = Path.Combine(s.Root, s.UninstallHook);
            if (File.Exists(path))
            {
                Ui.Step($"hook: {s.UninstallHook}");
                Exec.Run($"bash '{path}'", s.Root);
            }
        }

        if (remote)
        {
            var link = Path.Combine(Paths.QuickshellDir, s.Name);
            if (Catalog.IsRemoteLink(link))
                Directory.Delete(link);          // borra solo el enlace
            if (Directory.Exists(s.Root) && s.Root.StartsWith(Paths.RemoteShellsDir))
                Directory.Delete(s.Root, recursive: true);
            Ui.Step("clon borrado");
        }

        Console.WriteLine($"{Ui.Green("✓")} {s.Name} desinstalado");
        return 0;
    }
}