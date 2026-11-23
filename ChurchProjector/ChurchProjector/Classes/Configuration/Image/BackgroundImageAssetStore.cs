using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChurchProjector.Classes.Configuration.Image;

public sealed class BackgroundImageAsset
{
    private int _referenceCount = 1;

    public string Path { get; }
    public Bitmap Image { get; }

    internal BackgroundImageAsset(string path, Bitmap image)
    {
        Path = path;
        Image = image;
    }

    internal void AddReference()
    {
        while (true)
        {
            int currentCount = System.Threading.Volatile.Read(ref _referenceCount);
            if (currentCount == 0)
            {
                throw new ObjectDisposedException(nameof(BackgroundImageAsset));
            }

            if (System.Threading.Interlocked.CompareExchange(
                    ref _referenceCount,
                    currentCount + 1,
                    currentCount) == currentCount)
            {
                return;
            }
        }
    }

    internal void Release()
    {
        int remainingReferences = System.Threading.Interlocked.Decrement(ref _referenceCount);
        if (remainingReferences == 0)
        {
            Image.Dispose();
        }
        else if (remainingReferences < 0)
        {
            throw new InvalidOperationException("The background image asset was released more than once.");
        }
    }
}

public static class BackgroundImageAssetStore
{
    private static readonly string AssetDirectory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChurchProjector",
        "BackgroundImages");

    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, BackgroundImageAsset> Cache = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    public static BackgroundImageAsset Import(string sourcePath)
    {
        byte[] imageData;
        try
        {
            imageData = File.ReadAllBytes(sourcePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new BackgroundImageException("The selected background image could not be read.", ex);
        }

        Bitmap image = Decode(imageData, "The selected file is not a supported image.");
        string destinationPath = System.IO.Path.Combine(
            AssetDirectory,
            $"{Guid.NewGuid():N}{System.IO.Path.GetExtension(sourcePath)}");

        try
        {
            Directory.CreateDirectory(AssetDirectory);
            File.WriteAllBytes(destinationPath, imageData);

            BackgroundImageAsset asset = new(destinationPath, image);
            lock (CacheLock)
            {
                Cache.Add(destinationPath, asset);
            }

            return asset;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            image.Dispose();
            Exception failure = ex;
            try
            {
                File.Delete(destinationPath);
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                failure = new AggregateException(ex, cleanupException);
            }

            throw new BackgroundImageException("The background image could not be saved.", failure);
        }
    }

    public static BackgroundImageAsset Load(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        lock (CacheLock)
        {
            if (Cache.TryGetValue(fullPath, out BackgroundImageAsset? cached))
            {
                return cached;
            }

            byte[] imageData;
            try
            {
                imageData = File.ReadAllBytes(fullPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                throw new BackgroundImageException("The saved background image could not be read.", ex);
            }

            Bitmap image = Decode(imageData, "The saved background image is not a supported image.");
            BackgroundImageAsset asset = new(fullPath, image);
            Cache.Add(fullPath, asset);
            return asset;
        }
    }

    public static void DeleteIfManaged(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath = System.IO.Path.GetFullPath(path);
        string fullAssetDirectory = System.IO.Path.GetFullPath(AssetDirectory);
        string relativePath = System.IO.Path.GetRelativePath(fullAssetDirectory, fullPath);
        if (System.IO.Path.IsPathRooted(relativePath)
            || relativePath == ".."
            || relativePath.StartsWith($"..{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{System.IO.Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return;
        }

        lock (CacheLock)
        {
            if (Cache.Remove(fullPath, out BackgroundImageAsset? asset))
            {
                asset.Release();
            }

            File.Delete(fullPath);
        }
    }

    private static Bitmap Decode(byte[] imageData, string errorMessage)
    {
        try
        {
            using MemoryStream stream = new(imageData);
            return new Bitmap(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new BackgroundImageException(errorMessage, ex);
        }
    }
}

public sealed class BackgroundImageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
