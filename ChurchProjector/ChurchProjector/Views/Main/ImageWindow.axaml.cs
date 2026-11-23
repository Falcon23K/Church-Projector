using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ChurchProjector.Classes;
using ChurchProjector.Classes.Ndi;
using ChurchProjector.Views.Settings;
using Serilog;
using System;
using System.Linq;
using System.Threading;

namespace ChurchProjector.Views.Main;

/// <summary>
/// Interaction logic for EasyImageWindow.xaml
/// </summary>
public partial class ImageWindow : Window
{
    private const double BannerPadding = 10.0; // Should be the same as the clock
    private const int REPETITION_COUNT = 2;

    private NdiSender? _ndiSender;
    private DispatcherTimer? _ndiTimer;
    private RenderTargetBitmap? _ndiRenderTarget;
    private WriteableBitmap? _ndiFrame;
    private int _ndiWidth;
    private int _ndiHeight;
    private double _ndiScaling;
    private string? _ndiSourceName;
    private bool _ndiBlankFrameSent;
    private byte[]? _ndiTransparentPixels;

    public event Action<string>? NdiOutputFailed;

    public ImageViewModel ViewModel { get; }

    public void CreateBanner()
    {
        ViewModel.FirePropertyChanged();
       
        if (string.IsNullOrWhiteSpace(ViewModel.BannerText))
        {
            return;
        }

        ViewModel.IsBannerVisible = true;
        SpBanner.Children.Clear();
        for (int i = 0; i < REPETITION_COUNT; i++)
        {
            TextBlock bannerText = new()
            {
                Text = ViewModel.BannerText,
                FontSize = ViewModel.TextSize,
                Foreground = Brushes.White,
                Margin = new Thickness(BannerPadding),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            SpBanner.Children.Add(bannerText);
        }

        StartBannerAnimation();
    }

    CancellationTokenSource cancellationTokenSource = new();

    private void StartBannerAnimation()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource = new();
        CancellationToken token = cancellationTokenSource.Token;
        DispatcherTimer.Run(() =>
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
            TextBlock textBlock = SpBanner.Children.Cast<TextBlock>().FirstOrDefault()!;
            if (textBlock == null)
            {
                return false;
            }
            double bannerSpeed = GlobalConfig.JsonFile.Settings.BannerSettings.Speed / 10d;
            textBlock.Margin = new Thickness(textBlock.Margin.Left - bannerSpeed, BannerPadding, BannerPadding, BannerPadding);

            if (-textBlock.Margin.Left > textBlock.Bounds.Width + BannerPadding)
            {
                SpBanner.Children.Remove(textBlock);
                textBlock.Margin = new Thickness(BannerPadding);
                SpBanner.Children.Add(textBlock);
            }

            return SpBanner.IsVisible;
        }, TimeSpan.FromSeconds(1d / GlobalConfig.JsonFile.Settings.BannerSettings.Fps));
    }

    public ImageWindow()
    {
        ViewModel = null!;
        InitializeComponent();
    }

    public ImageWindow(SettingsViewModel settings)
    {
        InitializeComponent();
        DataContext = ViewModel = new ImageViewModel(settings);

        // Subcribe to position changes, this is because we can't bind the position.
        Position = ViewModel.Settings.Position;

        ViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
        ViewModel.Settings.PropertyChanged += SettingsViewModel_PropertyChanged;
        ViewModel.Settings.Settings.OutputDisplaySettings.PropertyChanged += OutputDisplaySettings_PropertyChanged;
    }

    public void StartNdiOutput()
    {
        ConfigureNdiOutput();
    }

    private void OutputDisplaySettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OutputDisplaySettings.NdiEnabled) or nameof(OutputDisplaySettings.NdiSourceName))
        {
            ConfigureNdiOutput();
        }
    }

    private void ConfigureNdiOutput()
    {
        OutputDisplaySettings settings = ViewModel.Settings.Settings.OutputDisplaySettings;
        string sourceName = settings.NdiSourceName?.Trim() ?? string.Empty;
        if (!settings.NdiEnabled)
        {
            StopNdiOutput();
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            StopNdiOutput();
            NdiOutputFailed?.Invoke(Lang.Resources.NdiSourceNameEmpty);
            return;
        }

        if (_ndiSender is not null && _ndiSourceName == sourceName)
        {
            return;
        }

        StopNdiOutput();
        try
        {
            var screen = ViewModel.Settings.SelectedMonitore.Value;
            Width = screen.Bounds.Width / screen.Scaling;
            Height = screen.Bounds.Height / screen.Scaling;

            _ndiSender = new NdiSender(sourceName);
            _ndiSender.SendFailed += NdiSender_SendFailed;
            _ndiSourceName = sourceName;
            _ndiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1d / 30) };
            _ndiTimer.Tick += NdiTimer_Tick;
            _ndiTimer.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to start the NDI output");
            StopNdiOutput();
            NdiOutputFailed?.Invoke(GetNdiErrorMessage(ex));
        }
    }

    private void NdiTimer_Tick(object? sender, EventArgs e)
    {
        if (_ndiSender is null)
        {
            return;
        }

        try
        {
            var screen = ViewModel.Settings.SelectedMonitore.Value;
            if (WindowState == WindowState.FullScreen &&
                (Width != screen.Bounds.Width / screen.Scaling ||
                 Height != screen.Bounds.Height / screen.Scaling))
            {
                Width = screen.Bounds.Width / screen.Scaling;
                Height = screen.Bounds.Height / screen.Scaling;
            }

            double scaling = screen.Scaling;
            int width = Math.Max(2, screen.Bounds.Width);
            int height = Math.Max(1, screen.Bounds.Height);
            if (width % 2 != 0)
            {
                width--;
            }

            if (width != _ndiWidth || height != _ndiHeight || scaling != _ndiScaling)
            {
                _ndiRenderTarget?.Dispose();
                _ndiFrame?.Dispose();

                PixelSize size = new(width, height);
                Vector dpi = new(96 * scaling, 96 * scaling);
                _ndiRenderTarget = new RenderTargetBitmap(size, dpi);
                _ndiFrame = new WriteableBitmap(size, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
                _ndiWidth = width;
                _ndiHeight = height;
                _ndiScaling = scaling;
                _ndiBlankFrameSent = false;
                _ndiTransparentPixels = null;
            }

            if (_ndiWidth == 0 || _ndiHeight == 0 || _ndiRenderTarget is null || _ndiFrame is null)
            {
                return;
            }

            if (!ViewModel.IsNdiTextSlide)
            {
                if (!_ndiBlankFrameSent)
                {
                    using ILockedFramebuffer blankFramebuffer = _ndiFrame.Lock();
                    _ndiTransparentPixels ??= new byte[
                        checked(blankFramebuffer.RowBytes * blankFramebuffer.Size.Height)];
                    System.Runtime.InteropServices.Marshal.Copy(
                        _ndiTransparentPixels,
                        0,
                        blankFramebuffer.Address,
                        _ndiTransparentPixels.Length);
                    _ndiBlankFrameSent = _ndiSender.SendFrame(blankFramebuffer);
                    if (_ndiBlankFrameSent)
                    {
                        _ndiTransparentPixels = null;
                    }
                }

                return;
            }

            _ndiBlankFrameSent = false;
            _ndiTransparentPixels = null;
            IBrush? originalBackground = Background;
            bool originalNdiRendering = ViewModel.IsNdiRendering;
            Background = Brushes.Transparent;
            ViewModel.IsNdiRendering = true;
            try
            {
                _ndiRenderTarget.Render(this);
            }
            finally
            {
                Background = originalBackground;
                ViewModel.IsNdiRendering = originalNdiRendering;
            }

            using ILockedFramebuffer framebuffer = _ndiFrame.Lock();
            _ndiRenderTarget.CopyPixels(framebuffer);
            _ndiSender.SendFrame(framebuffer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to render or send an NDI frame");
            StopNdiOutput();
            NdiOutputFailed?.Invoke(GetNdiErrorMessage(ex));
        }
    }

    private void NdiSender_SendFailed(Exception exception)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_ndiSender is null)
            {
                return;
            }

            Log.Error(exception, "The NDI sender failed");
            StopNdiOutput();
            NdiOutputFailed?.Invoke(GetNdiErrorMessage(exception));
        });
    }

    private static string GetNdiErrorMessage(Exception exception)
    {
        if (exception is DllNotFoundException)
        {
            return OperatingSystem.IsLinux()
                ? Lang.Resources.NdiRuntimeMissingLinux
                : Lang.Resources.NdiRuntimeMissing;
        }

        return string.Format(Lang.Resources.NdiOutputError, exception.Message);
    }

    internal void StopNdiOutput()
    {
        _ndiTimer?.Stop();
        if (_ndiTimer is not null)
        {
            _ndiTimer.Tick -= NdiTimer_Tick;
            _ndiTimer = null;
        }

        _ndiSender?.Dispose();
        _ndiSender = null;
        _ndiSourceName = null;
        _ndiRenderTarget?.Dispose();
        _ndiRenderTarget = null;
        _ndiFrame?.Dispose();
        _ndiFrame = null;
        _ndiWidth = 0;
        _ndiHeight = 0;
        _ndiBlankFrameSent = false;
        _ndiTransparentPixels = null;
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Position))
        {
            Position = ViewModel.Settings.Position;
        }
    }

    private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Settings.WindowState))
        {
            if (ViewModel.Settings.WindowState == WindowState.FullScreen)
            {
                var screen = ViewModel.Settings.SelectedMonitore.Value;
                Width = screen.Bounds.Width;
                Height = screen.Bounds.Height;
            }
        }
    }

    internal void StopBanner()
    {
        ViewModel.IsBannerVisible = false;
        SpBanner.Children.Clear();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.Settings.Settings.OutputDisplaySettings.PropertyChanged -= OutputDisplaySettings_PropertyChanged;
        StopNdiOutput();
        base.OnClosed(e);
    }
}
