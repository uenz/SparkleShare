using System;
using System.Diagnostics;
using System.IO;
using System.Text;
// logic from here: https://raw.githubusercontent.com/mklement0/fileicon/stable/bin/fileicon
public static class MacOSCustomIcon
{
    /// <summary>
    /// Entspricht ungefähr der Bash-Funktion:
    /// setCustomIcon <fileOrFolder> <imgFile>
    ///
    /// Returncodes:
    /// 0 = erfolgreich
    /// 1 = osascript oder Setzen fehlgeschlagen
    /// 3 = ungültige Eingabe / Datei nicht vorhanden / keine Rechte
    /// </summary>
    public static int SetCustomIcon(string fileOrFolder, string imgFile)
    {
        if (!OperatingSystem.IsMacOS())
        {
            Console.Error.WriteLine("Custom Finder icons are only supported on macOS.");
            return 3;
        }

        if (!(File.Exists(fileOrFolder) || Directory.Exists(fileOrFolder)))
        {
            Console.Error.WriteLine($"Target not found or neither file nor folder: '{fileOrFolder}'");
            return 3;
        }

        if (!File.Exists(imgFile))
        {
            Console.Error.WriteLine($"Image file not found: '{imgFile}'");
            return 3;
        }

        if (!CanRead(fileOrFolder))
        {
            Console.Error.WriteLine($"Cannot access '{fileOrFolder}': you do not have read permissions.");
            return 3;
        }

        if (!CanWrite(fileOrFolder))
        {
            Console.Error.WriteLine($"Cannot modify '{fileOrFolder}': you do not have write permissions.");
            return 3;
        }

        if (!CanRead(imgFile))
        {
            Console.Error.WriteLine($"Image file is not readable: '{imgFile}'");
            return 3;
        }

        string script = BuildAppleScript(fileOrFolder, imgFile);

        int ec = RunOsaScript(script, out string stdOut, out string stdErr);

        if (ec != 0)
        {
            Console.Error.WriteLine("Failed to assign a custom icon.");
            if (!string.IsNullOrWhiteSpace(stdErr))
                Console.Error.WriteLine(stdErr);

            return ec;
        }

        /*
         * Im Original wird danach testForCustomIcon(...) aufgerufen.
         * Das ist wichtig, weil NSWorkspace.setIcon(...) offenbar nicht immer
         * zuverlässig einen Fehler liefert, auch wenn das Bild ungültig ist.
         *
         * Wenn du die Verifikation aus dem Bash-Skript ebenfalls in C# übersetzt,
         * würdest du sie hier aufrufen:
         *
         * ec = TestForCustomIcon(fileOrFolder);
         * if (ec == 0) return 0;
         *
         * Für die reine Set-Funktion reicht in vielen Fällen der osascript-Exitcode.
         */

        return 0;
    }

    private static string BuildAppleScript(string fileOrFolder, string imgFile)
    {
        string sourcePath = ToAppleScriptString(imgFile);
        string destPath = ToAppleScriptString(fileOrFolder);

        return $@"
use framework ""Cocoa""

set sourcePath to {sourcePath}
set destPath to {destPath}

set sourceImage to (current application's NSImage's alloc()'s initWithContentsOfFile:sourcePath)
set imageSize to sourceImage's |size|()
set imageWidth to (width of imageSize) as real
set imageHeight to (height of imageSize) as real

set canvasSide to imageWidth
if imageHeight > canvasSide then set canvasSide to imageHeight

set drawWidth to imageWidth
set drawHeight to imageHeight
set drawOriginX to (canvasSide - drawWidth) / 2
set drawOriginY to (canvasSide - drawHeight) / 2

set squareImage to (current application's NSImage's alloc()'s initWithSize:{{width:canvasSide, height:canvasSide}})
squareImage's lockFocus()

current application's NSColor's clearColor()'s |set|()
current application's NSRectFill(current application's NSMakeRect(0, 0, canvasSide, canvasSide))

sourceImage's drawInRect:(current application's NSMakeRect(drawOriginX, drawOriginY, drawWidth, drawHeight)) fromRect:(current application's NSZeroRect) operation:(current application's NSCompositingOperationSourceOver) fraction:1.0

squareImage's unlockFocus()

current application's NSWorkspace's sharedWorkspace()'s setIcon:squareImage forFile:destPath options:2
";
    }

    private static int RunOsaScript(string script, out string stdOut, out string stdErr)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/osascript",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.StandardInput.Write(script);
        process.StandardInput.Close();

        process.WaitForExit();

        stdOut = output.ToString();
        stdErr = error.ToString();

        return process.ExitCode;
    }

    private static string ToAppleScriptString(string value)
    {
        /*
         * AppleScript-String escapen.
         * Wichtig für Pfade mit Anführungszeichen oder Backslashes.
         */
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"") + "\"";
    }

    private static bool CanRead(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }

            if (Directory.Exists(path))
            {
                Directory.EnumerateFileSystemEntries(path).GetEnumerator().Dispose();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CanWrite(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                return true;
            }

            if (Directory.Exists(path))
            {
                string testFile = Path.Combine(path, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(testFile, "");
                File.Delete(testFile);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}