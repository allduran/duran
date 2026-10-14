using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran info <shell> — lo que declara su shell.toml
public static class InfoCommand
{
    public static Command Create()
    {
        var shellArg = new Argument<string>("shell") { Description = "Nombre del shell" };
        var cmd = new Command("info", "Muestra lo que declara el shell.toml de un shell");
        cmd.Arguments.Add(shellArg);
        cmd.SetAction(parse => Run(parse.GetValue(shellArg)!));
        return cmd;
    }

    static int Run(string name)
    {
        var s = Catalog.Find(name);
        if (s == null)
        {
            Ui.Error($"No encuentro el shell '{name}'. Prueba 'duran list'.");
            return 1;
        }

        var running = Catalog.IsRunning(s.Name);
        Console.WriteLine($"{Ui.Bold(s.Name)} {Ui.Dim("v" + s.Version)}  {(running ? Ui.Red("● activo") : Ui.Dim("○ parado"))}");
        if (s.Description.Length > 0)
            Console.WriteLine(s.Description);
        Console.WriteLine();

        Row("origen", s.Source == ShellSource.Local ? "local" : "remoto (git)");
        Row("carpeta", Paths.Pretty(s.Root));
        Row("motor", s.Engine);
        Row("arrancar", s.Start);
        Row("parar", s.Stop);
        Row("paquetes", string.Join(" ", s.RepoPackages));
        if (s.AurPackages.Count > 0)
            Row("aur", string.Join(" ", s.AurPackages));
        Row("conflictos", string.Join(", ", s.ConflictShells));
        Row("instalar", s.InstallHook);
        Row("desinstalar", s.UninstallHook);

        if (s.Ipc.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(Ui.Bold("IPC"));
            var w = s.Ipc.Keys.Max(k => k.Length);
            foreach (var (key, command) in s.Ipc)
                Console.WriteLine($"  {Ui.Blue(key.PadRight(w))}  {Ui.Dim(command)}");
        }
        return 0;
    }

    static void Row(string label, string value)
    {
        if (value.Length > 0)
            Console.WriteLine($"  {Ui.Dim(label.PadRight(12))} {value}");
    }
}