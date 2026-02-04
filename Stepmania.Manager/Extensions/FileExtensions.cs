using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Stepmania.Manager.Models;

namespace Stepmania.Manager.Extensions;


public static class FileExtensions
{

    public static bool SafeContains(this string? value, string? match, bool ignorecase = true)
    {
        if (value == null) return false;
        if (match == null) return false;
        return value.Contains(match, ignorecase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
    }
    public static string WrapInQuotes(this string value)
    {
        return "\"" + value + "\"";
    }
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = false)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    public static string SmallestFile(this IEnumerable<string> files)
    {
        var currentSmallest = "";
        long? lastSize = null;
        foreach (var item in files)
        {
            var info = new FileInfo(item);
            if (lastSize == null || info.Length < lastSize)
            {
                lastSize = info.Length;
                currentSmallest = item;
            }
        }

        return currentSmallest;
    }

    public static bool ExtensionIs(this string fileName, string match)
    {
        return fileName?.EndsWith(match, StringComparison.CurrentCultureIgnoreCase) ?? false;
    }

    public static void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true); 
        }
    }

    private const string TitleCode = "#TITLE:";

    /// <summary>Appends a suffix to the #TITLE: line in an SM or DWI file (e.g. " [SMS Modded]").</summary>
    public static void AppendToTitleInStepFile(string filePath, string suffix)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || string.IsNullOrEmpty(suffix)) return;
        var lines = File.ReadAllLines(filePath);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(TitleCode, StringComparison.OrdinalIgnoreCase))
            {
                var line = lines[i];
                var idx = line.IndexOf(TitleCode, StringComparison.OrdinalIgnoreCase);
                var afterCode = line.Substring(idx + TitleCode.Length);
                var title = afterCode.Replace(";", "").Trim();
                if (title.EndsWith(suffix, StringComparison.Ordinal)) return;
                lines[i] = TitleCode + title + suffix + ";";
                break;
            }
        }
        File.WriteAllLines(filePath, lines);
    }

    private static readonly char[] StepChars = { '1', '2', '3', '4' };

    /// <summary>Removes jumps from an SM file: any row with 2+ simultaneous steps is reduced to a single step (leftmost).</summary>
    public static void RemoveJumpsFromSmFile(string smFilePath)
    {
        if (string.IsNullOrEmpty(smFilePath) || !File.Exists(smFilePath) || !smFilePath.ExtensionIs("sm")) return;
        var lines = File.ReadAllLines(smFilePath);
        var inNotes = false;
        var notesHeaderLinesLeft = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("#NOTES:", StringComparison.OrdinalIgnoreCase))
            {
                inNotes = true;
                notesHeaderLinesLeft = 5; // chart type, description, difficulty, meter, groove
                continue;
            }
            if (!inNotes) continue;
            if (notesHeaderLinesLeft > 0)
            {
                notesHeaderLinesLeft--;
                continue;
            }
            if (line.Trim() == ";")
            {
                inNotes = false;
                continue;
            }
            if (line.Trim() == ",") continue;
            var trimmed = line.Trim();
            if (trimmed.Length is not (4 or 5 or 8)) continue;
            var isNoteRow = true;
            foreach (var c in trimmed)
            {
                if (c != '0' && c != '1' && c != '2' && c != '3' && c != '4' && c != 'M' && c != 'K' && c != 'L' && c != 'F')
                {
                    isNoteRow = false;
                    break;
                }
            }
            if (!isNoteRow) continue;
            var stepCount = 0;
            foreach (var c in trimmed)
            {
                if (c == '1' || c == '2' || c == '3' || c == '4') stepCount++;
            }
            if (stepCount <= 1) continue;
            var firstStepIndex = -1;
            for (var j = 0; j < trimmed.Length; j++)
            {
                if (trimmed[j] == '1' || trimmed[j] == '2' || trimmed[j] == '3' || trimmed[j] == '4')
                {
                    firstStepIndex = j;
                    break;
                }
            }
            if (firstStepIndex < 0) continue;
            var sb = new System.Text.StringBuilder(trimmed.Length);
            for (var j = 0; j < trimmed.Length; j++)
                sb.Append(j == firstStepIndex ? trimmed[j] : '0');
            var start = line.IndexOf(trimmed, StringComparison.Ordinal);
            if (start >= 0)
                lines[i] = line.Remove(start, trimmed.Length).Insert(start, sb.ToString());
        }
        File.WriteAllLines(smFilePath, lines);
    }

    public static void OpenFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

    }

    public static async Task<bool> SwapOgg(Song danceFile)
    {
        
        var result = await OggToMp3(danceFile.MusicFile);
        if (result == false) return false;
        var oldSongFilePath = danceFile.MusicFile;
        var newSongFilePath = oldSongFilePath.Replace(".ogg", ".mp3", StringComparison.CurrentCultureIgnoreCase);

        if (danceFile.IsDwi())
        {
            var songFileSwapResult = await SwapAudio(newSongFilePath, danceFile.DwiFile);
            if (songFileSwapResult == false) return false;
        }

        if (danceFile.IsSm())
        {
            var songFileSwapResult = await SwapAudio(newSongFilePath, danceFile.SmFile);
            if (songFileSwapResult == false) return false;
        }

        File.Delete(oldSongFilePath);

        return true;
    }

    public static async Task<bool> SwapAudio(string audioFile, string danceFile)
    {
        try
        {
            //#FILE:Cat Song ~Theme of UPA~.mp3;
            //#MUSIC:Miami Sunset Drive.ogg;
            var isdwi = danceFile.ExtensionIs("dwi");
            if (string.IsNullOrEmpty(danceFile)) return false;
            var oldLines = await File.ReadAllLinesAsync(danceFile);
            var newLines = new List<string>();
            var replaceValue = isdwi ? "#FILE": "#MUSIC" ;
            var croppedFileName = audioFile.Substring(audioFile.LastIndexOf('\\') + 1); 
            for (var i = 0; i < oldLines.Length; i++)
            {
                var line = oldLines[i];
                var isReplace = line.Contains(replaceValue);
                if (isReplace)
                {
                    newLines.Add(replaceValue + ":" + croppedFileName);
                }
                else
                {
                    newLines.Add(line);
                }
            }

            await File.WriteAllLinesAsync(danceFile, newLines);
        }
        catch (Exception ex)
        {
            Debug.Write(ex.Message);
            return false;
        }

        return true;

    }

    public static async Task<bool> OggToMp3(string filename)
    {
        if (File.Exists(filename))
        {
            if (filename.ExtensionIs("ogg"))
            {
                var sansExtension = filename.Substring(0, filename.Length - 4);
                var mp3 = sansExtension + ".mp3";

                //ProcessStartInfo startInfo = new ProcessStartInfo
                //{
                //    Arguments = "-i \"" + sansExtension + "\".{ogg,mp3}",
                //    FileName = "ffmpeg.exe",
                //    UseShellExecute = true
                //};

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = $"-i  {filename.WrapInQuotes()} {mp3.WrapInQuotes()}",
                    FileName = "ffmpeg.exe"
                };

                var process = Process.Start(startInfo);
                await process?.WaitForExitAsync();
                var code = process?.ExitCode;
                return  code == 0;
            }
 
        }

        return false;
    }


}