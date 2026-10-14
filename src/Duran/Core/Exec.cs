using System.Diagnostics;
using Duran.Manifest;

namespace Duran.Core;

/// Ejecuta comandos de shell (sh -c) y arranca/para shells.
public static class Exec
{
    /// Ejecuta y espera. Devuelve el código de salida.
    public static int Run(string command, bool quiet = false)
    {
        var psi = new ProcessStartInfo("sh", ["-c", command])
        {
            UseShellExecute = false,
            RedirectStandardOutput = quiet,
            RedirectStandardError = quiet,
        };
        using var p = Process.Start(psi)!;
        if (quiet)
        {
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
        }
        p.WaitForExit();
        return p.ExitCode;
    }

    public static string StartCommand(ShellManifest s) => s.Start.Length > 0 ? s.Start : $"qs -c {s.Name} -d";
    public static string StopCommand(ShellManifest s) => s.Stop.Length > 0 ? s.Stop : $"qs -c {s.Name} kill";

    /// Espera (hasta `timeoutMs`) a que el shell esté o no corriendo.
    public static bool WaitRunning(string name, bool running, int timeoutMs = 4000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (Catalog.IsRunning(name) == running)
                return true;
            Thread.Sleep(100);
        }
        return Catalog.IsRunning(name) == running;
    }

    public static bool Start(ShellManifest s)
    {
        if (Catalog.IsRunning(s.Name))
            return true;
        // A /dev/null y sin redirigir: el demonio (-d) heredaría las tuberías y nos dejaría esperando
        Run(StartCommand(s) + " >/dev/null 2>&1");
        return WaitRunning(s.Name, true);
    }

    public static bool Stop(ShellManifest s)
    {
        if (!Catalog.IsRunning(s.Name))
            return true;
        Run(StopCommand(s) + " >/dev/null 2>&1");
        return WaitRunning(s.Name, false);
    }
}