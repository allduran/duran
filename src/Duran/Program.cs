using System.CommandLine;
using Duran.Commands;

var root = new RootCommand("duran — instala y cambia shells de Quickshell a partir de su shell.toml");
root.Subcommands.Add(ListCommand.Create());
root.Subcommands.Add(InfoCommand.Create());

return root.Parse(args).Invoke();