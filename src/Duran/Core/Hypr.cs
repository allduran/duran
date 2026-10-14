using Duran.Manifest;

namespace Duran.Core;

/// Enlaza la parte de Hyprland del shell activo en ~/.config/hypr/shell.lua
/// (tu hyprland.lua la carga con pcall(require, "shell")) y recarga Hyprland.
public static class Hypr
{
    public static string LinkPath => Path.Combine(Paths.ConfigHome, "hypr", "shell.lua");

    /// Apunta shell.lua al [hyprland].config del shell (o lo quita si no declara ninguno).
    public static void Apply(ShellManifest s)
    {
        var target = s.HyprConfig.Length > 0 ? Path.Combine(s.Root, s.HyprConfig) : null;
        if (target != null && !File.Exists(target))
        {
            Ui.Warn($"{s.Name} declara {s.HyprConfig} pero no existe");
            target = null;
        }
        if (CurrentTarget() == target)
            return;                         // ya estaba así: no recargar
        if (!RemoveLink())
            return;
        if (target != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LinkPath)!);
            File.CreateSymbolicLink(LinkPath, target);
            Ui.Step($"hyprland: {Paths.Pretty(LinkPath)} → {s.Name}");
        }
        Reload();
    }

    /// Quita el enlace si apunta a este shell (al desinstalarlo).
    public static void Remove(ShellManifest s)
    {
        var current = CurrentTarget();
        if (current == null || !current.StartsWith(s.Root + "/"))
            return;
        if (RemoveLink())
        {
            Ui.Step("hyprland: config del shell quitada");
            Reload();
        }
    }

    static string? CurrentTarget()
    {
        var info = new FileInfo(LinkPath);
        if (info.LinkTarget == null)
            return null;
        return Path.GetFullPath(info.LinkTarget, Path.GetDirectoryName(LinkPath)!);
    }

    /// Borra shell.lua solo si es un enlace (un archivo tuyo no se toca).
    static bool RemoveLink()
    {
        var info = new FileInfo(LinkPath);
        if (info.LinkTarget != null)
        {
            info.Delete();
            return true;
        }
        if (info.Exists)
        {
            Ui.Warn($"{Paths.Pretty(LinkPath)} es un archivo normal; no lo toco");
            return false;
        }
        return true;
    }

    static void Reload()
    {
        if (Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE") is { Length: > 0 })
            Exec.Run("hyprctl reload >/dev/null 2>&1");
    }
}
