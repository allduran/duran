using System.CommandLine;
using Duran.Core;
using Duran.Manifest;

namespace Duran.Commands;

/// duran update [shell] — git pull de los shells remotos; reinicia los que estén corriendo.
public static class UpdateCommand
{
    public static Command Create()
    {
        var shellArg = new Argument<string?>("shell")
        {
            Description = "Shell a actualizar (por defecto, todos los remotos)",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var cmd = new Command("update", "Actualiza los shells instalados desde git");
        cmd.Arguments.Add(shellArg);
        cmd.SetAction(p => Run(p.GetValue(shellArg)));
        return cmd;
    }

    static int Run(string? name)
    {
        var shells = Catalog.Discover()
            .Where(s => name == null || s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (shells.Count == 0)
        {
            Ui.Error(name == null ? "No hay shells." : $"No encuentro el shell '{name}'.");
            return 1;
        }

        var failed = 0;
        foreach (var s in shells)
        {
            if (s.Source == ShellSource.Local)
            {
                if (name != null)
                    Ui.Step($"{s.Name}: es local, se actualiza con tus dotfiles (git pull en ~/dotfiles)");
                continue;
            }

            Ui.Step($"{s.Name}: git pull");
            if (Exec.Run("git pull --ff-only --quiet", s.Root) != 0)
            {
                Ui.Warn($"{s.Name}: no se pudo actualizar (¿cambios locales en el clon?)");
                failed++;
                continue;
            }
            if (Catalog.IsRunning(s.Name))
            {
                Ui.Step($"{s.Name}: reiniciando");
                Exec.Stop(s);
                Exec.Start(s);
            }
        }

        Console.WriteLine(failed == 0 ? $"{Ui.Green("✓")} al día" : $"{Ui.Yellow("!")} {failed} sin actualizar");
        return failed == 0 ? 0 : 1;
    }
}