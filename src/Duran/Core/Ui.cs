namespace Duran.Core;

/// Salida con color (se desactiva con NO_COLOR o si no es una terminal).
public static class Ui
{
    static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;

    static string Wrap(string code, string text) => Color ? $"\e[{code}m{text}\e[0m" : text;

    public static string Bold(string t) => Wrap("1", t);
    public static string Dim(string t) => Wrap("2", t);
    public static string Red(string t) => Wrap("1;31", t);
    public static string Blue(string t) => Wrap("1;34", t);
    public static string Yellow(string t) => Wrap("1;33", t);
    public static string Green(string t) => Wrap("1;32", t);

    public static void Error(string msg) => Console.Error.WriteLine($"{Red("✕")} {msg}");
    public static void Warn(string msg) => Console.Error.WriteLine($"{Yellow("!")} {msg}");
    public static void Step(string msg) => Console.WriteLine($"  {Blue("·")} {msg}");
}