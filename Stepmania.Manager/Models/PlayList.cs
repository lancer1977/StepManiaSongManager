using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace Stepmania.Manager.Models;

public class PlayList
{
    public string Name { get; set; }
    public string FolderName { get; set; }
    private SourceList<Song> _sourceItems { get; } = new SourceList<Song>();
    public ReadOnlyObservableCollection<Song> _items;
    public ReadOnlyObservableCollection<Song> Items => _items;
    public override string ToString() => Name;

    public PlayList()
    {
        _sourceItems.Connect()
            .Sort(SortExpressionComparer<Song>.Ascending(x => x.ArtistName).ThenByAscending(x => x.Title))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .Subscribe();
    }

    public PlayList(string name, IEnumerable<Song> songs):this()
    {
        Name = name;
        _sourceItems.AddRange(songs);
    }

    public void Add(Song song)
    {
        _sourceItems.Add(song);
    }

    public void Remove(Song song)
    {
        _sourceItems.Remove(song);
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

    public async Task Refresh()
    {
        var items = new List<Song>();
        foreach (var item in Directory.GetDirectories(FolderName))
        {
            var song = await Song.ParseSong(item);
            if (song == null) continue;
            items.Add(song);
        }
        _sourceItems.Edit(x =>
        {
            x.Clear();
            x.AddRange(items);
        });


    }
}