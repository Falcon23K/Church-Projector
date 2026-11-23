using SkiaSharp;
using System;
using System.IO;

namespace ChurchProjector.Classes.Configuration.Image;

public sealed record BackgroundImageAsset(string Path, SKImage Image);

public static class BackgroundImageAssetStore
{
    private static readonly string AssetDirectory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChurchProjector",
        "BackgroundImages");

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

        SKImage image = SKImage.FromEncodedData(imageData)
            ?? throw new BackgroundImageException("The selected file is not a supported image.");

        string destinationPath = System.IO.Path.Combine(
            AssetDirectory,
            $"{Guid.NewGuid():N}{System.IO.Path.GetExtension(sourcePath)}");

        try
        {
            Directory.CreateDirectory(AssetDirectory);
            File.WriteAllBytes(destinationPath, imageData);
            return new BackgroundImageAsset(destinationPath, image);
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

    public static SKImage Load(string path)
    {
        try
        {
            byte[] imageData = File.ReadAllBytes(path);
            return SKImage.FromEncodedData(imageData)
                ?? throw new BackgroundImageException("The saved background image is not a supported image.");
        }
        catch (BackgroundImageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new BackgroundImageException("The saved background image could not be read.", ex);
        }
    }

    public static void DeleteIfManaged(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
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

        File.Delete(fullPath);
    }
}

public sealed class BackgroundImageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
