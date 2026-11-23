using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ChurchProjector.Views.Song;
public partial class SongSearch : Window
{
    private readonly SongSearchViewModel _viewModel;

    public SongSearch()
    {
        InitializeComponent();

        _viewModel = new SongSearchViewModel()
        {
            CloseDialogCommand = new RelayCommand(Close),
            OpenSongCommand = new RelayCommand(() =>
            {
                if (_viewModel is null || _viewModel.SelectedSong is null)
                {
                    return;
                }
                OpenSong?.Invoke(_viewModel.SelectedSong);
                Close();
            }),
        };
        DataContext = _viewModel;
        TxtSearchBox.AttachedToVisualTree += TxtShortcut_AttachedToVisualTree;
    }

    public required Action<ChurchProjector.Classes.Song> OpenSong { get; set; }

    // https://github.com/AvaloniaUI/Avalonia/issues/4835#issuecomment-706530463
    private void TxtShortcut_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        TxtSearchBox.Focus();
    }

    private void ListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (e.Source is StyledElement se && se.DataContext is ChurchProjector.Classes.Song)
        {
            _viewModel.OpenSongCommand.Execute(null);
        }
    }
}
