using System.CommandLine;
using Duran.Core;

namespace Duran.Commands;

/// duran new <nombre> [--dir carpeta] — crea un shell mínimo listo para 'duran switch'.
/// Con --dir (p. ej. en tus dotfiles) lo crea ahí y lo enlaza en ~/.config/quickshell/<nombre>.
public static class NewCommand
{
    public static Command Create()
    {
        var nameArg = new Argument<string>("nombre") { Description = "Nombre del shell (minúsculas, sin espacios)" };
        var dirOpt = new Option<string?>("--dir") { Description = "Carpeta donde crearlo (por defecto ~/.config/quickshell/<nombre>)" };
        var cmd = new Command("new", "Crea un shell nuevo con shell.toml, shell.qml y hooks");
        cmd.Arguments.Add(nameArg);
        cmd.Options.Add(dirOpt);
        cmd.SetAction(p => Run(p.GetValue(nameArg)!, p.GetValue(dirOpt)));
        return cmd;
    }

    static int Run(string name, string? dir)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z0-9][a-z0-9_-]*$"))
        {
            Ui.Error("Usa minúsculas, números, '-' o '_' (p. ej. 'ancient').");
            return 1;
        }

        var link = Path.Combine(Paths.QuickshellDir, name);
        var root = dir != null ? Path.GetFullPath(Path.Combine(Paths.Expand(dir), name)) : link;
        if (Directory.Exists(root) || Directory.Exists(link) || Catalog.Find(name) != null)
        {
            Ui.Error($"Ya existe un shell '{name}'.");
            return 1;
        }

        Console.WriteLine($"{Ui.Bold("duran new")} {Ui.Bold(name)}");
        Directory.CreateDirectory(Path.Combine(root, "hooks"));
        Write(root, "shell.toml", Toml(name));
        Write(root, "shell.qml", Qml(name));
        Write(root, "hooks/install.sh", Hook("install", name), exec: true);
        Write(root, "hooks/uninstall.sh", Hook("uninstall", name), exec: true);

        if (root != link)
        {
            Directory.CreateDirectory(Paths.QuickshellDir);
            Directory.CreateSymbolicLink(link, root);
            Ui.Step($"enlazado en {Paths.Pretty(link)}");
        }

        Console.WriteLine($"{Ui.Green("✓")} creado en {Paths.Pretty(root)}");
        Console.WriteLine(Ui.Dim($"  pruébalo: duran switch {name}"));
        return 0;
    }

    static void Write(string root, string rel, string content, bool exec = false)
    {
        var path = Path.Combine(root, rel);
        File.WriteAllText(path, content);
        if (exec)
            File.SetUnixFileMode(path, File.GetUnixFileMode(path) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        Ui.Step(rel);
    }

    static string Toml(string n) => $$"""
        [shell]
        name = "{{n}}"
        description = ""
        version = "0.1.0"
        engine = "quickshell"

        [packages]
        repo = ["quickshell"]
        aur = []

        [ipc]
        bar = "qs -c {{n}} ipc call bar toggle"

        [conflicts]
        shells = []

        [hooks]
        install = "hooks/install.sh"
        uninstall = "hooks/uninstall.sh"

        """;

    static string Qml(string n) => $$"""
        // {{n}} — shell de Quickshell creado con 'duran new'
        import Quickshell
        import Quickshell.Io
        import QtQuick

        ShellRoot {
            id: root
            property bool barVisible: true

            Variants {
                model: Quickshell.screens

                PanelWindow {
                    required property var modelData
                    screen: modelData
                    visible: root.barVisible
                    anchors { top: true; left: true; right: true }
                    implicitHeight: 32
                    color: "#16161e"

                    Text {
                        anchors.centerIn: parent
                        text: "{{n}}"
                        color: "#c0caf5"
                        font.pixelSize: 14
                    }
                }
            }

            // duran ipc bar
            IpcHandler {
                target: "bar"
                function toggle(): void { root.barVisible = !root.barVisible }
            }
        }

        """;

    static string Hook(string kind, string n) => $$"""
        #!/usr/bin/env bash
        # {{n}} — hook de {{(kind == "install" ? "instalación" : "desinstalación")}}: lo que no son paquetes
        # (servicios, grupos, carpetas de estado…). Se ejecuta desde la carpeta del shell.
        set -euo pipefail

        echo "{{n}}: {{kind}} ok"

        """;
}