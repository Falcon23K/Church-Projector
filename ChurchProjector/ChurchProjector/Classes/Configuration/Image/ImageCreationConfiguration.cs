using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchProjector.Classes.Configuration.Image;

public class ImageCreationConfiguration : ObservableObject
{
    private readonly object _backgroundImageLock = new();
    private BackgroundImageAsset? _backgroundImageAsset;
    private string? _cachedBackgroundImagePath;
    private string? _backgroundImagePath;

    public string? BackgroundImagePath
    {
        get => _backgroundImagePath;
        set
        {
            lock (_backgroundImageLock)
            {
                if (_backgroundImagePath != value)
                {
                    _backgroundImageAsset?.Release();
                    _backgroundImageAsset = null;
                    _cachedBackgroundImagePath = null;
                    SetProperty(ref _backgroundImagePath, value);
                }
            }
        }
    }

    public BackgroundImageAsset? GetBackgroundImageAsset()
    {
        lock (_backgroundImageLock)
        {
            if (string.IsNullOrWhiteSpace(_backgroundImagePath))
            {
                return null;
            }

            if (_backgroundImageAsset is not null
                && _cachedBackgroundImagePath == _backgroundImagePath)
            {
                return _backgroundImageAsset;
            }

            _backgroundImageAsset?.Release();
            _backgroundImageAsset = BackgroundImageAssetStore.Load(_backgroundImagePath);
            _backgroundImageAsset.AddReference();
            _cachedBackgroundImagePath = _backgroundImagePath;
            return _backgroundImageAsset;
        }
    }

    public void SetBackgroundImage(string? path, BackgroundImageAsset? imageAsset)
    {
        lock (_backgroundImageLock)
        {
            imageAsset?.AddReference();
            _backgroundImageAsset?.Release();
            _backgroundImageAsset = imageAsset;
            _cachedBackgroundImagePath = path;
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