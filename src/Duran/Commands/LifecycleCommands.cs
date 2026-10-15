using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran start / stop / restart / status — siempre sobre el shell activo (o el que se indique)
public static class LifecycleCommands
{
    public static IEnumerable<Command> Create()
    {
        yield return Make("start", "Arranca el shell activo (para el autostart de Hyprland)", s => { Hypr.Apply(s); return Exec.Start(s); }, "arrancado");
        yield return Make("stop", "Para el shell activo", s => Exec.Stop(s), "parado");
        yield return Make("restart", "Reinicia el shell activo", s => Exec.Stop(s) && Exec.Start(s), "reiniciado");

        var status = new Command("status", "Muestra el shell activo y si está corriendo");
        status.SetAction(_ => Status());
        yield return status;
    }

    static Command Make(string verb, string description, Func<ShellManifest, bool> action, string done)
    {
        var shellArg = new Argument<string?>("shell")
        {
            Description = "Shell (por defecto, el activo)",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var cmd = new Command(verb, description);
        cmd.Arguments.Add(shellArg);
        cmd.SetAction(parse =>
        {
            var s = Resolve(parse.GetValue(shellArg));
            if (s == null)
                return 1;
            if (!action(s))
            {
                Ui.Error($"no se pudo: {verb} {s.Name}");
                return 1;
            }
            Console.WriteLine($"{Ui.Green("✓")} {s.Name} {done}");
            return 0;
        });
        return cmd;
    }

    /// El shell indicado o, si no hay, el activo (o el único que haya).
    public static ShellManifest? Resolve(string? name)
    {
        name ??= State.Active;
        if (name == null)
        {
            var all = Catalog.Discover();
            if (all.Count == 1)
                return all[0];
            Ui.Error("No hay shell activo. Usa 'duran switch <shell>' primero.");
            return null;
        }
        var s = Catalog.Find(name);
        if (s == null)
            Ui.Error($"No encuentro el shell '{name}'. Prueba 'duran list'.");
        return s;
    }

    static int Status()
    {
        var active = State.Active;
        if (active == null)
        {
            Console.WriteLine(Ui.Dim("Sin shell activo (usa 'duran switch <shell>')"));
            return 0;
        }
        var running = Catalog.IsRunning(active);
        Console.WriteLine($"{Ui.Bold(active)}  {(running ? Ui.Red("● corriendo") : Ui.Dim("○ parado"))}");
        return 0;
    }
}