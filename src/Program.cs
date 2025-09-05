using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            MessageBox(IntPtr.Zero, 
                string.Join(Environment.NewLine,
                    "Usage: SetUserEnv <path_to_env_file>",
                    "",
                    "Placeholders:",
                    " $CURRENT_PATH$\t| Current path.",
                    " $USER_PROFILE$\t| User profile path.",
                    " $TEMP$        \t| Temp path.",
                    "",
                    "You can bind this program to the .env extension to import quickly"
                ),
                "No .env file specified", 0x0);
            return;
        }

        string envFile = args[0];

        if (!File.Exists(envFile))
        {
            MessageBox(IntPtr.Zero, $"File not found: {envFile}", "Error", 0x0);
            return;
        }

        var commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["$CURRENT_PATH$"] = Path.GetDirectoryName(envFile) ?? string.Empty,
            ["$USER_PROFILE$"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ["$TEMP$"] = Path.GetTempPath(),
        };

        string[] lines;
        try
        {
            lines = File.ReadAllLines(envFile);
        }
        catch (Exception ex)
        {
            MessageBox(IntPtr.Zero, $"Failed to read file: {ex.Message}", "Error", 0x0);
            return;
        }

        var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#") || line.TrimStart().StartsWith(";"))
                continue;

            var parts = line.Split(new char[] { '=' }, 2);
            if (parts.Length != 2)
                continue;

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            foreach (var cmd in commands)
                value = value.Replace(cmd.Key, cmd.Value);

            envVars[key] = value;
        }

        // Preview
        var sb = new StringBuilder();
        foreach (var kv in envVars.OrderBy(k => k.Key))
            sb.AppendLine($"{kv.Key}={kv.Value}");

        int result = MessageBox(IntPtr.Zero,
            "The following variables will be set:\n\n" + sb +
            "\nDo you want to continue?", "Preview", 0x4);

        if (result != 6) // IDYES
            return;

        foreach (var envVar in envVars)
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value, EnvironmentVariableTarget.User);

        MessageBox(IntPtr.Zero, "Variables have been set for the current user.", "Done", 0x0);
    }
}
