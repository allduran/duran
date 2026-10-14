using System.CommandLine;
using Duran.Core;

namespace Duran.Commands;

/// duran completions fish  → imprime el script de autocompletado
/// duran __names shells|ipc → (oculto) nombres que usa ese script
public static class CompletionsCommand
{
    public static IEnumerable<Command> Create()
    {
        var shellArg = new Argument<string>("shell") { Description = "Solo 'fish' por ahora" };
        var completions = new Command("completions", "Imprime el autocompletado (duran completions fish > ~/.config/fish/completions/duran.fish)");
        completions.Arguments.Add(shellArg);
        completions.SetAction(p =>
        {
            if (p.GetValue(shellArg) != "fish")
            {
                Ui.Error("Solo hay autocompletado para fish.");
                return 1;
            }
            Console.Write(Fish);
            return 0;
        });

        var kindArg = new Argument<string>("tipo");
        var names = new Command("__names") { Hidden = true };
        names.Arguments.Add(kindArg);
        names.SetAction(p =>
        {
            switch (p.GetValue(kindArg))
            {
                case "shells":
                    foreach (var s in Catalog.Discover())
                        Console.WriteLine($"{s.Name}\t{s.Description}");
                    break;
                case "ipc":
                    if (LifecycleCommands.Resolve(null) is { } shell)
                        foreach (var key in shell.Ipc.Keys.Order())
                            Console.WriteLine(key);
                    break;
            }
            return 0;
        });

        return [completions, names];
    }

    const string Fish = """
        # duran — autocompletado para fish (generado con: duran completions fish)
        complete -c duran -f

        set -l __duran_cmds list ls info install uninstall update switch start stop restart status ipc new completions
        function __duran_shells; duran __names shells 2>/dev/null; end
        function __duran_ipc; duran __names ipc 2>/dev/null; end

        # Subcomandos
        set -l nosub "not __fish_seen_subcommand_from $__duran_cmds"
        complete -c duran -n $nosub -a list -d 'Shells disponibles'
        complete -c duran -n $nosub -a info -d 'Lo que declara su shell.toml'
        complete -c duran -n $nosub -a install -d 'Instala un shell (nombre, url o usuario/repo)'
        complete -c duran -n $nosub -a uninstall -d 'Desinstala un shell'
        complete -c duran -n $nosub -a update -d 'git pull de los shells remotos'
        complete -c duran -n $nosub -a switch -d 'Cambia al shell indicado'
        complete -c duran -n $nosub -a start -d 'Arranca el shell activo'
        complete -c duran -n $nosub -a stop -d 'Para el shell activo'
        complete -c duran -n $nosub -a restart -d 'Reinicia el shell activo'
        complete -c duran -n $nosub -a status -d 'Shell activo y si está corriendo'
        complete -c duran -n $nosub -a ipc -d 'Acción IPC del shell activo'
        complete -c duran -n $nosub -a new -d 'Crea un shell nuevo'
        complete -c duran -n $nosub -a completions -d 'Script de autocompletado'

        # Argumentos
        complete -c duran -n '__fish_seen_subcommand_from info install uninstall update switch start stop restart' -a '(__duran_shells)'
        complete -c duran -n '__fish_seen_subcommand_from ipc; and test (count (commandline -opc)) -eq 2' -a '(__duran_ipc)'
        complete -c duran -n '__fish_seen_subcommand_from completions' -a fish

        # Opciones
        complete -c duran -n '__fish_seen_subcommand_from install' -l no-packages -d 'No instalar paquetes'
        complete -c duran -n '__fish_seen_subcommand_from install uninstall' -l no-hook -d 'No ejecutar el hook'
        complete -c duran -n '__fish_seen_subcommand_from install' -s s -l switch -d 'Cambiar a él al terminar'
        complete -c duran -n '__fish_seen_subcommand_from uninstall' -s y -l yes -d 'No preguntar'
        complete -c duran -n '__fish_seen_subcommand_from new' -l dir -r -F -d 'Carpeta donde crearlo'

        """;
}