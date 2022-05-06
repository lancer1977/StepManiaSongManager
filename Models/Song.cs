using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PolyhydraGames.Extensions;
using Stepmania.Manager.Extensions;

namespace Stepmania.Manager.Models;

public class Song : INotifyPropertyChanged
{
    public string DuplicateFolder { get; set; }
    public string Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            OnPropertyChanged();
        }
    }

    private static string artistCode = "#ARTIST:";
    private static string titleCode = "#TITLE:";
    private static string bannerCode = "#BANNER:";
    private static string backgroundCode = "#BACKGROUND:";
    private bool _moved;
    private string _color;

    public bool Moved
    {
        get => _moved;
        set
        {
            if (_moved == value) return;
            _moved = value;
            UpdateColor();
            OnPropertyChanged();
        }
    }

    private void UpdateColor()
    {
        Color = Moved ? "Red" : "Cyan";
    }

    public string ArtistName { get; set; }
    public string Title { get; set; }

    public string RootDirectory { get; set; }
    public bool IsMP3 { get; set; }
    public string PartialDirectory { get; set; }
    public string StepFile { get; set; }

    public string SmFile { get; set; }


    
    public string StepBeginner { get; set; }
    public string StepEasy { get; set; }
    public string StepMedium { get; set; }
    public string StepHard { get; set; }
    public string StepChallenge { get; set; }


    public string MusicFile { get; set; }
    public string VideoFile { get; set; }

    public string ThumbNailFile { get; set; }
    public string BackgroundFile { get; set; }

    public string DwiFile { get; set; }
    public string DwiBeginner { get; set; }

    public string DwiBasic { get; set; }
    public string DwiAnother { get; set; }
    public string DwiManiac { get; set; }

    

    public string Pad(string value)
    {
        return $" {value} ";
    }
    public async Task ParseAsync()
    {
        try
        {
            await ParseStepManiaFile(SmFile);
            await ParseDwiFile(DwiFile);
            IsMP3 = MusicFile.ExtensionIs("mp3");
            UpdateColor();
            StepFile = SmFile ?? DwiFile;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }


    }
    public async Task ParseStepManiaFile(string fileLocation)
    {
        if (string.IsNullOrEmpty(fileLocation)) return;
        var file = await File.ReadAllLinesAsync(fileLocation);


        for (var i = 0; i < file.Length; i++)
        {
            var line = file[i];

            if (i < 30)
            {

                if (line.Contains(artistCode))
                {
                    ArtistName = line.Replace(artistCode, "").Replace(";", "");
                    continue;
                }

                if (line.Contains(titleCode))
                {
                    Title = line.Replace(titleCode, "").Replace(";", "");
                    continue;
                }

                if (line.Contains(bannerCode))
                {
                    var fileName = line.Replace(bannerCode, "").Replace(";", "");
                    ThumbNailFile = RootDirectory + "\\" + fileName;
                    continue;
                }

                if (line.Contains(backgroundCode))
                {
                    BackgroundFile = line.Replace(backgroundCode, "").Replace(";", "");
                    continue;
                }
            }



            if (line.Contains("easy:", StringComparison.CurrentCultureIgnoreCase))
            {
                StepEasy = Pad(file[i + 1].ToInt().ToString());
                continue;
            }

            if (line.Contains("medium:", StringComparison.CurrentCultureIgnoreCase))
            {
                StepMedium = Pad(file[i + 1].ToInt().ToString()); 
                continue;
            }
            if (line.Contains("beginner:", StringComparison.CurrentCultureIgnoreCase))
            {
                StepBeginner = Pad(file[i + 1].ToInt().ToString()); 
                continue;
            }
            if (line.Contains("hard:", StringComparison.CurrentCultureIgnoreCase))
            {
                StepHard = Pad(file[i + 1].ToInt().ToString());
            }
            if (line.Contains("challenge:", StringComparison.CurrentCultureIgnoreCase))
            {
                StepChallenge = Pad(file[i + 1].ToInt().ToString());
            }
        }

    }
    public async Task ParseDwiFile(string fileLocation)
    {
        if (string.IsNullOrEmpty(fileLocation)) return;
        var file = await File.ReadAllLinesAsync(fileLocation);

        for (var i = 0; i < file.Length; i++)
        {
            var line = file[i];


            if (line.Contains(artistCode))
            {
                ArtistName = line.Replace(artistCode, "").Replace(";", "");

            }
            if (line.Contains(titleCode))
            {
                Title = line.Replace(titleCode, "").Replace(";", "");

            }


            if (Title?.Contains("LITTLE BOY") ?? false)
            {

            }
            if (line.Contains("SINGLE:BEGINNER"))
            {
                DwiBeginner = Pad(line.Substring(line.IndexOf("BEGINNER:") + 9, 1));
                continue;

            }
            if (line.Contains("SINGLE:BASIC"))
            {
                DwiBasic = Pad(line.Substring(line.IndexOf("BASIC:") + 6, 1));
                continue;

            }

            if (line.Contains("SINGLE:ANOTHER"))
            {
                DwiAnother = Pad(line.Substring(line.IndexOf("ANOTHER:") + 8, 1));
                continue;
            }
            if (line.Contains("SINGLE:MANIAC"))
            {
                DwiManiac = Pad(line.Substring(line.IndexOf("MANIAC:") + 7, 1));
                continue;
            }

        }

    }

    public static async Task<Song> ParseSong(string directoryName)
    {
        try
        {

            var files = Directory.GetFiles(directoryName);
            var dwi = files.FirstOrDefault(x => x.ExtensionIs("dwi"));
            var sm = files.FirstOrDefault(x => x.ExtensionIs("sm")) ?? null;
            var mp3 = files.FirstOrDefault(x => x.ExtensionIs("mp3") || x.ExtensionIs("ogg"));

            var images = files.Where(x => x.ExtensionIs("png") || x.ExtensionIs("bmp"));
            var videos = files.FirstOrDefault(x => x.ExtensionIs("avi") || x.ExtensionIs("mp4"));
            var smallest = images.SmallestFile();

            var song = new Song
            {
                RootDirectory = directoryName,
                DwiFile = dwi,
                SmFile = sm,
                MusicFile = mp3,
                ThumbNailFile = smallest,
                VideoFile = videos,
                PartialDirectory = directoryName.Substring(directoryName.LastIndexOf("\\") + 1)
            };
            await song.ParseAsync();

            return song;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public static class SongExtensions
{
    public static bool IsDwi(this Song song)
    {
        return !string.IsNullOrEmpty(song.DwiFile);
    }

    public static bool IsSm(this Song song)
    {
        return !string.IsNullOrEmpty(song.SmFile);
    }
}