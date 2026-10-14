using System.CommandLine;
using Duran.Core;

namespace Duran.Commands;

/// duran switch <shell> — para los shells que estén corriendo, arranca el elegido y lo deja como activo
public static class SwitchCommand
{
    public static Command Create()
    {
        var shellArg = new Argument<string>("shell") { Description = "Shell al que cambiar" };
        var cmd = new Command("switch", "Cambia al shell indicado (para el actual y arranca el nuevo)");
        cmd.Arguments.Add(shellArg);
        cmd.SetAction(parse => Run(parse.GetValue(shellArg)!));
        return cmd;
    }

    public static int Run(string name)
    {
        var shells = Catalog.Discover();
        var target = shells.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            Ui.Error($"No encuentro el shell '{name}'. Prueba 'duran list'.");
            return 1;
        }

        Console.WriteLine($"{Ui.Bold("duran")} → {Ui.Bold(target.Name)}");

        // Parar los demás (y los conflictos declarados, aunque no tengan shell.toml)
        foreach (var s in shells.Where(s => s != target && Catalog.IsRunning(s.Name)))
        {
            Ui.Step($"parando {s.Name}");
            if (!Exec.Stop(s))
                Ui.Warn($"{s.Name} sigue corriendo");
        }
        foreach (var c in target.ConflictShells.Where(Catalog.IsRunning))
        {
            Ui.Step($"parando {c} (conflicto)");
            Exec.Run($"qs -c {c} kill >/dev/null 2>&1");
        }

        State.Active = target.Name;

        if (Catalog.IsRunning(target.Name))
        {
            Ui.Step($"{target.Name} ya estaba corriendo");
            return 0;
        }
        Ui.Step($"arrancando {target.Name}");
        if (!Exec.Start(target))
        {
            Ui.Error($"{target.Name} no arrancó. Mira su log: qs log -c {target.Name}");
            return 1;
        }
        Console.WriteLine($"{Ui.Green("✓")} {target.Name} activo");
        return 0;
    }
}