using System.CommandLine;
using System.Text.RegularExpressions;
using Duran.Core;

namespace Duran.Commands;

/// duran ipc <acción> [args…] — ejecuta la acción [ipc] del shell activo.
/// Así los atajos de Hyprland no dependen de qué shell esté puesto:
///   duran ipc launcher      → qs -c velvet ipc call launcher toggle
///   duran ipc mode_set dev  → qs -c velvet ipc call mode set dev
public static partial class IpcCommand
{
    public static Command Create()
    {
        var actionArg = new Argument<string?>("acción")
        {
            Description = "Clave de [ipc] en el shell.toml (sin nada: lista las acciones)",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var restArg = new Argument<string[]>("args")
        {
            Description = "Argumentos para la acción (sustituyen al <marcador>)",
            Arity = ArgumentArity.ZeroOrMore,
        };
        var cmd = new Command("ipc", "Ejecuta una acción IPC del shell activo");
        cmd.Arguments.Add(actionArg);
        cmd.Arguments.Add(restArg);
        cmd.SetAction(parse => Run(parse.GetValue(actionArg), parse.GetValue(restArg) ?? []));
        return cmd;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Placeholder();

    static int Run(string? action, string[] args)
    {
        var shell = LifecycleCommands.Resolve(null);
        if (shell == null)
            return 1;

        if (action == null)
        {
            var w = shell.Ipc.Keys.DefaultIfEmpty("").Max(k => k.Length);
            foreach (var (key, line) in shell.Ipc)
                Console.WriteLine($"  {Ui.Blue(key.PadRight(w))}  {Ui.Dim(line)}");
            return 0;
        }

        if (!shell.Ipc.TryGetValue(action, out var template))
        {
            Ui.Error($"{shell.Name} no tiene la acción '{action}'. Mira 'duran ipc'.");
            return 1;
        }

        var extra = string.Join(" ", args);
        var command = Placeholder().IsMatch(template)
            ? Placeholder().Replace(template, extra, 1)
            : (extra.Length > 0 ? template + " " + extra : template);
        return Exec.Run(command);
    }
}