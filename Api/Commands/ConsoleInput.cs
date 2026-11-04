namespace Api.Commands;

/// <summary>Shared helpers for the one-time console commands (create-admin, reset-password).</summary>
internal static class ConsoleInput
{
    public static string? Option(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Asks for a new password twice (hidden). Returns null and prints why when it is not valid.</summary>
    public static string? ReadNewPassword()
    {
        var password = ReadHidden("Contraseña nueva (mínimo 8 caracteres): ");
        if (password.Length < 8)
        {
            Console.Error.WriteLine("La contraseña debe tener al menos 8 caracteres.");
            return null;
        }
        if (!Console.IsInputRedirected && ReadHidden("Repite la contraseña: ") != password)
        {
            Console.Error.WriteLine("Las contraseñas no coinciden.");
            return null;
        }
        return password;
    }

    /// <summary>Reads a line without echoing it; falls back to a plain line when input is piped.</summary>
    private static string ReadHidden(string prompt)
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;

        Console.Write(prompt);
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); continue; }
            if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
        }
        Console.WriteLine();
        return new string(chars.ToArray());
    }
}
