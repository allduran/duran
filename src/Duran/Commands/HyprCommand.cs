using System.CommandLine;
using Duran.Core;

namespace Duran.Commands;

/// duran hypr init — instala o pone al día la config base de Hyprland
public static class HyprCommand
{
    public static Command Create()
    {
        var yes = new Option<bool>("--yes", "-y") { Description = "No preguntar antes de sustituir tu config" };
        var init = new Command("init", "Instala o pone al día la config base de Hyprland (atajos de duran, autoarranque, tema)");
        init.Options.Add(yes);
        init.SetAction(p => Starter.Init(p.GetValue(yes)));

        var cmd = new Command("hypr", "Config de Hyprland para los shells de duran");
        cmd.Subcommands.Add(init);
        return cmd;
    }
}