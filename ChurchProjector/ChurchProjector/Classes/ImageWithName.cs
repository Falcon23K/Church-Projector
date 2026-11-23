using Avalonia.Media.Imaging;
using System;

namespace ChurchProjector.Classes;
public class ImageWithName(string title, Bitmap image, bool isOverflowing, bool isTextSlide = false) : IDisposable
{
    public string Title { get; set; } = title;
    public Bitmap Image { get; set; } = image;
    public bool IsOverflowing { get; set; } = isOverflowing;
    public bool IsTextSlide { get; set; } = isTextSlide;

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
        }

        _disposed = true;
    }
}
