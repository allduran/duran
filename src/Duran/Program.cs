using System.CommandLine;
using Duran.Commands;

var root = new RootCommand("duran — instala y cambia shells de Quickshell a partir de su shell.toml");
root.Subcommands.Add(ListCommand.Create());
root.Subcommands.Add(InfoCommand.Create());
root.Subcommands.Add(SwitchCommand.Create());
foreach (var c in LifecycleCommands.Create())
    root.Subcommands.Add(c);
root.Subcommands.Add(IpcCommand.Create());

return root.Parse(args).Invoke();