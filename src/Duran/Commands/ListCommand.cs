using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran list — shells disponibles y cuál está corriendo
public static class ListCommand
{
    public static Command Create()
    {
        var cmd = new Command("list", "Muestra los shells disponibles y cuál está activo");
        cmd.Aliases.Add("ls");
        cmd.SetAction(_ => Run());
        return cmd;
    }

    static int Run()
    {
        var errors = new List<string>();
        var shells = Catalog.Discover(errors);

        if (shells.Count == 0)
        {
            Console.WriteLine("No hay shells con shell.toml en " + Paths.Pretty(Paths.QuickshellDir));
        }
        else
        {
            var w = shells.Max(s => s.Name.Length);
            foreach (var s in shells)
            {
                var running = Catalog.IsRunning(s.Name);
                var mark = running ? Ui.Red("●") : Ui.Dim("○");
                var name = running ? Ui.Bold(s.Name.PadRight(w)) : s.Name.PadRight(w);
                var source = s.Source == ShellSource.Local ? Ui.Blue("local ") : Ui.Yellow("remoto");
                var version = Ui.Dim((s.Version.Length > 0 ? "v" + s.Version : "").PadRight(8));
                Console.WriteLine($" {mark} {name}  {version} {source}  {s.Description}");
            }
        }

        foreach (var e in errors)
            Ui.Warn(e);
        return 0;
    }
}