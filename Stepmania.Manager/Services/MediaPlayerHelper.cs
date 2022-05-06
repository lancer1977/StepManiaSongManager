using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace Stepmania.Manager.Services;

public class MediaPlayerHelper : IMediaPlayer
{
    private MediaPlayer _mediaPlayer = new MediaPlayer();
    public void PlayFile(string x)
    {
        if (x.EndsWith("ogg"))
        {
            try
            {
                System.Diagnostics.Process.Start(x);
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

        }
        else
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            if (File.Exists(x) == false) return;
            _mediaPlayer.Open(new Uri(x));
            _mediaPlayer.Play();
        }

    }

    public void Stop()
    {
        _mediaPlayer.Stop();
        _mediaPlayer.Close();
    }
}