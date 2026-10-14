using System.Diagnostics;
using Duran.Manifest;

namespace Duran.Core;

/// Ejecuta comandos de shell (sh -c) y arranca/para shells.
public static class Exec
{
    /// Ejecuta y espera, con la terminal heredada (se ven las preguntas de sudo, paru…).
    public static int Run(string command, string? workDir = null)
    {
        var psi = new ProcessStartInfo("sh", ["-c", command]) { UseShellExecute = false };
        if (workDir != null)
            psi.WorkingDirectory = workDir;
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }

    /// Ejecuta y devuelve (código, salida estándar).
    public static (int Code, string Output) Capture(string command)
    {
        var psi = new ProcessStartInfo("sh", ["-c", command])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output);
    }

    /// ¿Existe el programa en el PATH?
    public static bool Has(string program) => Capture($"command -v {program}").Code == 0;

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
        // A /dev/null: el demonio (-d) no debe quedarse con nuestra terminal
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