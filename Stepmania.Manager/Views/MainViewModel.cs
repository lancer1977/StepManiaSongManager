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
using Prism.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Stepmania.Manager.Dialogs;
using Stepmania.Manager.Extensions;
using Stepmania.Manager.Models;
using Stepmania.Manager.Services;

namespace Stepmania.Manager.Views;

public class MainViewModel : ViewModelAsyncBase
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
    public ICommand DuplicateAsSmsModdedCommand { get; }
    public ICommand RemoveJumpsCommand { get; }

    public bool IsLoaded { [ObservableAsProperty] get; }
    /// <summary>StepMania Instance root path (the installation you are editing).</summary>
    [Reactive] public string StepManiaFolder { get; set; }
    /// <summary>Song Repository root path (independent store of songs, e.g. network folder).</summary>
    [Reactive] public string SongRepositoryFolder { get; set; }
    [Reactive] public string SearchText { get; set; }
    [Reactive] public PlayList MovingPlayListSelection { get; set; }
    [Reactive] public PlayList SelectedPlayList { get; set; }
    [Reactive] public PlayList SelectedRepositoryPlayList { get; set; }

    private SourceList<PlayList> _sourceItems { get; } = new SourceList<PlayList>();
    public ReadOnlyObservableCollection<PlayList> _items;
    public ReadOnlyObservableCollection<PlayList> Items => _items;

    private SourceList<PlayList> _repositorySourceItems { get; } = new SourceList<PlayList>();
    public ReadOnlyObservableCollection<PlayList> _repositoryItems;
    public ReadOnlyObservableCollection<PlayList> RepositoryItems => _repositoryItems;

    /// <summary>Songs currently displayed (from instance playlist or repository category).</summary>
    public ReadOnlyObservableCollection<Song> DisplayedSongs => SelectedRepositoryPlayList?.Items ?? SelectedPlayList?.Items ?? EmptySongs;
    /// <summary>Title for the current view (playlist or repository category name).</summary>
    public string DisplayedTitle => SelectedRepositoryPlayList != null ? "Repository: " + SelectedRepositoryPlayList.Name : SelectedPlayList?.Name ?? "";
    /// <summary>True when viewing a repository category (read-only store).</summary>
    public bool IsRepositoryView => SelectedRepositoryPlayList != null;

    public ICommand RefreshRepositoryCommand { get; }
    public ICommand CopyFromRepositoryToInstanceCommand { get; }


    private static readonly ReadOnlyObservableCollection<Song> EmptySongs = new ReadOnlyObservableCollection<Song>(new ObservableCollection<Song>());

    public MainViewModel(IDialogService dialogService, IMediaPlayer mediaPlayer)
    {
        _dialogService = dialogService;
        _mediaPlayer = mediaPlayer;
        var connection = _sourceItems.Connect();

        connection
            .Sort(SortExpressionComparer<PlayList>.Ascending(x => x.Name))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _items)
            .Subscribe();

        var repoConnection = _repositorySourceItems.Connect();
        repoConnection
            .Sort(SortExpressionComparer<PlayList>.Ascending(x => x.Name))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _repositoryItems)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedPlayList).Subscribe(FindDuplicates);
        this.WhenAnyValue(x => x.SelectedPlayList).Where(x => x != null).Subscribe(_ => SelectedRepositoryPlayList = null);
        this.WhenAnyValue(x => x.SelectedRepositoryPlayList).Where(x => x != null).Subscribe(_ => SelectedPlayList = null);
        this.WhenAnyValue(x => x.SelectedPlayList, x => x.SelectedRepositoryPlayList).Subscribe(_ =>
        {
            this.RaisePropertyChanged(nameof(DisplayedSongs));
            this.RaisePropertyChanged(nameof(DisplayedTitle));
            this.RaisePropertyChanged(nameof(IsRepositoryView));
        });
        connection.CountChanged().ManySelect(x => true).ToPropertyEx(this, x => x.IsLoaded);
        CreatePlayListCommand = ReactiveCommand.CreateFromTask(CreatePlaylist);
        //ReactiveUIExtensions.RegisterErrorCallback(async (x, y) =>
        //{
        //    Debug.WriteLine(x + y + "");
        //    await dialogService.GetString(x);
        //});

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
        DuplicateAsSmsModdedCommand = ReactiveCommand.CreateFromTask<Song>(DuplicateAsSmsModded).OnException("Player Errored");
        RemoveJumpsCommand = ReactiveCommand.CreateFromTask<Song>(RemoveJumpsFromSm).OnException("Player Errored");

        PlayCommand = ReactiveCommand.Create<string>(PlayAction).OnException("Player Errored");
        RefreshListCommand = ReactiveCommand.CreateFromTask(RefreshListAction);
        RefreshRepositoryCommand = ReactiveCommand.CreateFromTask(RefreshRepositoryAction);
        CopyFromRepositoryToInstanceCommand = ReactiveCommand.CreateFromTask<Song>(CopyFromRepositoryToInstance).OnException("Copy failed");

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

    private const string SmsModdedSuffix = " [SMS Modded]";

    private async Task DuplicateAsSmsModded(Song song)
    {
        var parentDir = Path.GetDirectoryName(song.RootDirectory);
        if (string.IsNullOrEmpty(parentDir) || !Directory.Exists(parentDir)) return;

        var playlist = SelectedPlayList?.FolderName == parentDir ? SelectedPlayList : Items.FirstOrDefault(p => p.FolderName == parentDir);
        if (playlist == null) return;

        var baseName = song.PartialDirectory + SmsModdedSuffix;
        var newFolderName = baseName;
        var counter = 1;
        while (Directory.Exists(Path.Combine(parentDir, newFolderName)))
        {
            counter++;
            newFolderName = song.PartialDirectory + SmsModdedSuffix + " (" + counter + ")";
        }

        var destPath = Path.Combine(parentDir, newFolderName);
        FileExtensions.CopyDirectory(song.RootDirectory, destPath);

        if (!string.IsNullOrEmpty(song.SmFile))
        {
            var newSmPath = Path.Combine(destPath, Path.GetFileName(song.SmFile));
            FileExtensions.AppendToTitleInStepFile(newSmPath, SmsModdedSuffix);
        }
        if (!string.IsNullOrEmpty(song.DwiFile))
        {
            var newDwiPath = Path.Combine(destPath, Path.GetFileName(song.DwiFile));
            FileExtensions.AppendToTitleInStepFile(newDwiPath, SmsModdedSuffix);
        }

        var newSong = await Song.ParseSong(destPath);
        playlist.Add(newSong);
    }

    private static Task RemoveJumpsFromSm(Song song)
    {
        if (string.IsNullOrEmpty(song.SmFile) || !File.Exists(song.SmFile)) return Task.CompletedTask;
        FileExtensions.RemoveJumpsFromSmFile(song.SmFile);
        return Task.CompletedTask;
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

    private async Task RefreshRepositoryAction()
    {
        if (string.IsNullOrEmpty(SongRepositoryFolder) || !Directory.Exists(SongRepositoryFolder)) return;
        var items = await PlayListActions.GetPlayLists(SongRepositoryFolder);
        _repositorySourceItems.Edit(x =>
        {
            x.Clear();
            x.AddRange(items);
        });
    }

    private async Task CopyFromRepositoryToInstance(Song song)
    {
        if (MovingPlayListSelection == null) return;
        var targetDir = MovingPlayListSelection.FolderName;
        if (!Directory.Exists(targetDir)) return;
        var combinedTarget = Path.Combine(targetDir, song.PartialDirectory);
        FileExtensions.CopyDirectory(song.RootDirectory, combinedTarget);
        var newSong = await Song.ParseSong(combinedTarget);
        MovingPlayListSelection.Add(newSong);
    }



    public async Task SyncSongs(string source, string destination)
    {
        //1. Get a list of source directories
        //2. Get a list of destination directories
        //
        //var sourceDirectories = 
    }

}

