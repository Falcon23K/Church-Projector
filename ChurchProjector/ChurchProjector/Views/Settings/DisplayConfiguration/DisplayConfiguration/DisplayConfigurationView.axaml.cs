using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ChurchProjector.Classes.Configuration.Image;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChurchProjector.Views.Settings.DisplayConfiguration.DisplayConfiguration;
/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class DisplayConfigurationView : UserControl
{
    public DisplayConfigurationView()
    {
        InitializeComponent();
    }

    private async void OnSelectBackgroundImageClick(object? sender, RoutedEventArgs e)
    {
        BackgroundImageErrorMessage.IsVisible = false;
        if (DataContext is not ImageCreationConfiguration configuration)
        {
            return;
        }

        TopLevel topLevel = TopLevel.GetTopLevel(this)
            ?? throw new InvalidOperationException("The background image picker is not attached to a window.");
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(Lang.Resources.Images)
                    {
                        Patterns = ["*.bmp", "*.gif", "*.jpeg", "*.jpg", "*.png", "*.webp"]
                    }
                ]
            });

        if (files.Count == 1)
        {
            BackgroundImageAsset asset;
            try
            {
                asset = BackgroundImageAssetStore.Import(files[0].Path.LocalPath);
            }
            catch (BackgroundImageException ex)
            {
                Log.Error(ex, "Unable to import a background image");
                BackgroundImageErrorMessage.Text = Lang.Resources.BackgroundImageImportFailed;
                BackgroundImageErrorMessage.IsVisible = true;
                return;
            }

            string? previousPath = configuration.BackgroundImagePath;
            configuration.SetBackgroundImage(asset.Path, asset.Image);
            DeletePreviousImage(previousPath);
        }
    }

    private void OnRemoveBackgroundImageClick(object? sender, RoutedEventArgs e)
    {
        BackgroundImageErrorMessage.IsVisible = false;
        if (DataContext is ImageCreationConfiguration configuration)
        {
            string? previousPath = configuration.BackgroundImagePath;
            configuration.SetBackgroundImage(null, null);
            DeletePreviousImage(previousPath);
        }
    }

    private static void DeletePreviousImage(string? path)
    {
        try
        {
            BackgroundImageAssetStore.DeleteIfManaged(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Log.Warning(ex, "Unable to delete an unused background image");
        }
    }
}
