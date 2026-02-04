using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Prism.Dialogs;
using Stepmania.Manager.Dialogs;

namespace Stepmania.Manager.Models;

public static class PlayListActions
{
    public static async Task<PlayList?> CreatePlayList(string rootFolder, IDialogService dialogs)
    {
        if (string.IsNullOrEmpty(rootFolder)) return null; 
        var result = await dialogs.GetString("Playlist Name");
        var path = Path.Combine(rootFolder, "Songs");
        path = Path.Combine(path, result);
        Directory.CreateDirectory(path);
        var playList = await ParsePlayList(path);
        return playList ?? null;
    }
    public static async Task<List<PlayList>> GetPlayLists(string directoryName)
    {
        var list = new List<PlayList>();
        var directories = Directory.GetDirectories(directoryName);
        foreach (var item in directories)
        {
            var playlist = await ParsePlayList(item);
            if (playlist == null) continue;
            list.Add(playlist);
        }
        return list;
    }

    public static async Task<PlayList?> ParsePlayList(string directoryName)
    {
        var playList = new PlayList();
        if (!Directory.Exists(directoryName)) return null;
        playList.Name = directoryName.Substring(directoryName.LastIndexOf('\\') + 1);
        playList.FolderName = directoryName;
        await playList.Refresh();

        return playList;
    }
}