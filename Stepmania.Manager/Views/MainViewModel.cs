using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using PolyhydraGames.Core.ReactiveUI;
using Prism.Services.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Stepmania.Manager.Dialogs;
using Stepmania.Manager.Extensions;
using Stepmania.Manager.Models;
using Stepmania.Manager.Services;

namespace Stepmania.Manager.Views;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IMediaPlayer _mediaPlayer;

    public ICommand ReplaceOggCommand { get; }
    public ICommand AllOggToMp3Command { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshListCommand { get; }
    public ICommand OpenVideoCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand StopMusicCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand DWICleanupCommand { get; }
    public ICommand CreatePlayListCommand { get; }

    public bool IsLoaded { [ObservableAsProperty] get; }
    [Reactive] public string StepManiaFolder { get; set; }
    [Reactive] public string SearchText { get; set; }
    [Reactive] public PlayList MovingPlayListSelection { get; set; }
    [Reactive] public PlayList SelectedPlayList { get; set; }

    private SourceList<PlayList> _sourceItems { get; } = new SourceList<PlayList>();
    public ReadOnlyObservableCollection<PlayList> _items;
    public ReadOnlyObservableCollection<PlayList> Items => _items;


    public MainViewModel(IDialogService dialogService, IMediaPlayer mediaPlayer)
    {
        DialogServiceExtensions.DialogSerivce = dialogService;
        _dialogService = dialogService;
        _mediaPlayer = mediaPlayer;
        var connection = _sourceItems.Connect();

        connection
            .Sort(SortExpressionComparer<PlayList>.Ascending(x => x.Name))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .Subscribe();
        this.WhenAnyValue(x => x.SelectedPlayList).Subscribe(FindDuplicates);
        connection.CountChanged().ManySelect(x => true).ToPropertyEx(this, x => x.IsLoaded);
        CreatePlayListCommand = ReactiveCommand.CreateFromTask(CreatePlaylist);
        ReactiveUIExtensions.RegisterErrorCallback(async (x, y) =>
        {
            Debug.WriteLine(x + y + "");
            await dialogService.GetString(x);
        });

        DWICleanupCommand = ReactiveCommand.Create(DWICleanup);
        SearchCommand = ReactiveCommand.Create(SearchAction).OnException();
        OpenFolderCommand = ReactiveCommand.Create<string>(FileExtensions.OpenFolder);
        DeleteCommand = ReactiveCommand.CreateFromTask<Song>(async x =>
        {
            if (await _dialogService.GetConfirmation("Are you sure you want to delete this song?"))
            {
                FileExtensions.DeleteFolder(x.RootDirectory);
                if (SelectedPlayList != null)
                    await SelectedPlayList.Refresh();
            };
        }).OnException();
        ReplaceOggCommand = ReactiveCommand.CreateFromTask<Song>(ReplaceOgg).OnException("Player Errored");
        AllOggToMp3Command = ReactiveCommand.CreateFromTask(ReplaceAllOgg).OnException("Player Errored");
        StopMusicCommand = ReactiveCommand.Create<string>(x =>
        {
            _mediaPlayer.Stop();

        }).OnException("Stop Errored");

        MoveCommand = ReactiveCommand.CreateFromTask<Song>(MoveSong).OnException("Player Errored");
        CopyCommand = ReactiveCommand.CreateFromTask<Song>(CopySong).OnException("Player Errored");

        PlayCommand = ReactiveCommand.Create<string>(PlayAction).OnException("Player Errored");
        RefreshListCommand = ReactiveCommand.CreateFromTask(async () => { await RefreshListAction(); });

        OpenVideoCommand = ReactiveCommand.Create<string>((x) =>
        {
            try
            {
                Process.Start(x);
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }


        });
        StepManiaFolder = "F:\\FakeStepMania";
    }

    private async Task CreatePlaylist()
    {
        var result = await PlayListActions.CreatePlayList(StepManiaFolder, _dialogService);
        if (result != null)
            _sourceItems.Add(result);
    }

    private void SearchAction()
    {
        var songs = Items.SelectMany(x => x.Items).Where(x => x.Title.SafeContains(SearchText) || x.ArtistName.SafeContains(SearchText));
        SelectedPlayList = new PlayList(SearchText, songs);
    }

    public ReactiveCommand<Unit, Unit> SearchCommand { get; set; }

    private async Task ReplaceOgg(Song song)
    {
        try
        {
            await FileExtensions.SwapOgg(song);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    public void FindDuplicates(PlayList? list)
    {
        if (list == null) return;
        try
        {
            var playListSongs = list.Items.ToList();
            var otherPlaylistSongs = Items.Except(new[] { list }).SelectMany(x => x.Items).ToList();
            foreach (var item in playListSongs)
            {
                var duplicate = otherPlaylistSongs.FirstOrDefault(x => x.ArtistName == item.ArtistName && x.Title == item.Title);
                if (duplicate != null)
                {
                    item.DuplicateFolder = duplicate.RootDirectory;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private async Task ReplaceAllOgg()
    {
        foreach (var item in Items)
        {
            foreach (var song in item.Items)
            {
                if (song.IsMP3) continue;
                await ReplaceOgg(song);
            }
        }
    }
    private void DWICleanup()
    {
        foreach (var item in _sourceItems.Items)
        {
            foreach (var song in item.Items)
            {
                if (string.IsNullOrEmpty(song.DwiFile) == false && string.IsNullOrEmpty(song.SmFile) == false)
                {
                    File.Move(song.DwiFile, song.DwiFile + ".unused");
                }
            }
        }

        RefreshListCommand?.Execute(null);
    }



    private async Task MoveSong(Song x)
    {

        var targetDir = MovingPlayListSelection.FolderName;
        if (Directory.Exists(targetDir))
        {
            var combinedTarget = Path.Combine(targetDir, x.PartialDirectory);

            Directory.Move(x.RootDirectory, combinedTarget);
            //FileExtensions.OpenFolder(combinedTarget);
            SelectedPlayList.Remove(x);
            var newSong = await Song.ParseSong(combinedTarget);
            MovingPlayListSelection.Add(newSong);
        }
    }
    private async Task CopySong(Song x)
    {

        var targetDir = MovingPlayListSelection.FolderName;
        if (Directory.Exists(targetDir))
        {
            var combinedTarget = Path.Combine(targetDir, x.PartialDirectory);
            FileExtensions.CopyDirectory(x.RootDirectory, combinedTarget);
            var newSong = await Song.ParseSong(combinedTarget);
            MovingPlayListSelection.Add(newSong);
        }
    }
    private void PlayAction(string x)
    {
        _mediaPlayer.PlayFile(x);
    }

    private async Task RefreshListAction()
    {
        var directory = Path.Combine(StepManiaFolder, "Songs");
        if (Directory.Exists(directory))
        {

            var items = await PlayListActions.GetPlayLists(directory);
            _sourceItems.Edit(x =>
            {
                x.Clear();
                x.AddRange(items);
            });
        }
    }



    public async Task SyncSongs(string source, string destination)
    {
        //1. Get a list of source directories
        //2. Get a list of destination directories
        //
        //var sourceDirectories = 
    }

}

