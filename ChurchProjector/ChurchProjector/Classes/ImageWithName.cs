using Avalonia.Media.Imaging;
using ChurchProjector.Classes.Configuration.Image;
using System;

namespace ChurchProjector.Classes;
public class ImageWithName : IDisposable
{
    public ImageWithName(
        string title,
        Bitmap image,
        bool isOverflowing,
        bool isTextSlide = false,
        BackgroundImageAsset? backgroundImageAsset = null)
    {
        Title = title;
        Image = image;
        IsOverflowing = isOverflowing;
        IsTextSlide = isTextSlide;
        BackgroundImageAsset = backgroundImageAsset;
        BackgroundImageAsset?.AddReference();
    }

    public string Title { get; set; }
    public Bitmap Image { get; set; }
    public bool IsOverflowing { get; set; }
    public bool IsTextSlide { get; set; }
    public BackgroundImageAsset? BackgroundImageAsset { get; }
    public Bitmap? BackgroundImage => BackgroundImageAsset?.Image;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Image.Dispose();
            BackgroundImageAsset?.Release();
        }

        _disposed = true;
    }
}
