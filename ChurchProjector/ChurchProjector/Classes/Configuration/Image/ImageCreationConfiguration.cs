using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace ChurchProjector.Classes.Configuration.Image;

public class ImageCreationConfiguration : ObservableObject
{
    private readonly object _backgroundImageLock = new();
    private SKImage? _backgroundImage;
    private string? _backgroundImagePath;

    public string? BackgroundImagePath
    {
        get => _backgroundImagePath;
        set
        {
            lock (_backgroundImageLock)
            {
                if (SetProperty(ref _backgroundImagePath, value))
                {
                    _backgroundImage?.Dispose();
                    _backgroundImage = null;
                }
            }
        }
    }

    public SKImage? GetBackgroundImage()
    {
        lock (_backgroundImageLock)
        {
            if (string.IsNullOrWhiteSpace(_backgroundImagePath))
            {
                return null;
            }

            return _backgroundImage ??= BackgroundImageAssetStore.Load(_backgroundImagePath);
        }
    }

    public void SetBackgroundImage(string? path, SKImage? image)
    {
        lock (_backgroundImageLock)
        {
            _backgroundImage?.Dispose();
            _backgroundImage = image;
            SetProperty(ref _backgroundImagePath, path, nameof(BackgroundImagePath));
        }
    }

    public ImageCreationItemConfiguration Header
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    public ImageCreationItemConfiguration Content
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    public ImageCreationItemConfiguration Bottom
    {
        get;
        set => SetProperty(ref field, value);
    } = new();
}