using Avalonia.Platform;
using Tractus.Ndi;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace ChurchProjector.Classes.Ndi;

public sealed class NdiSender : IDisposable
{
    private static readonly object RuntimeLock = new();
    private static bool _wrapperInitialized;
    private static bool _runtimeInitialized;

    private readonly Channel<QueuedFrame> _frames = Channel.CreateBounded<QueuedFrame>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });
    private nint _sender;
    private readonly Task _sendTask;

    public event Action<Exception>? SendFailed;

    public NdiSender(string sourceName)
    {
        InitializeRuntime();

        using NDIInteropString ndiName = new(sourceName);
        send_create_t settings = new()
        {
            p_ndi_name = ndiName,
            clock_video = true,
            clock_audio = false,
        };
        _sender = NDIWrapper.send_create(ref settings);
        if (_sender == nint.Zero)
        {
            throw new InvalidOperationException("The NDI sender could not be created.");
        }

        _sendTask = Task.Run(SendFramesAsync);
    }

    internal static void ShutdownRuntime()
    {
        lock (RuntimeLock)
        {
            if (!_runtimeInitialized)
            {
                return;
            }

            NDIWrapper.destroy();
            _runtimeInitialized = false;
        }
    }

    private static void InitializeRuntime()
    {
        lock (RuntimeLock)
        {
            if (_runtimeInitialized)
            {
                return;
            }

            if (!_wrapperInitialized)
            {
                NDIWrapper.Initialize(useAdvancedDynLib: false, exactLibLookupPath: NdiRuntimeLocator.FindRuntimeLibrary());
                _wrapperInitialized = true;
            }

            if (!NDIWrapper.initialize())
            {
                throw new InvalidOperationException("The NDI runtime does not support this CPU.");
            }

            _runtimeInitialized = true;
        }
    }

    public bool SendFrame(ILockedFramebuffer framebuffer)
    {
        int length = checked(framebuffer.RowBytes * framebuffer.Size.Height);
        byte[] pixels = ArrayPool<byte>.Shared.Rent(length);
        bool queued = false;
        try
        {
            Marshal.Copy(framebuffer.Address, pixels, 0, length);
            queued = _frames.Writer.TryWrite(new QueuedFrame(pixels, framebuffer.Size.Width, framebuffer.Size.Height, framebuffer.RowBytes));
        }
        finally
        {
            if (!queued)
            {
                ArrayPool<byte>.Shared.Return(pixels);
            }
        }

        return queued;
    }

    private async Task SendFramesAsync()
    {
        await foreach (QueuedFrame queuedFrame in _frames.Reader.ReadAllAsync())
        {
            GCHandle pin = GCHandle.Alloc(queuedFrame.Pixels, GCHandleType.Pinned);
            try
            {
                video_frame_v2_t frame = new()
                {
                    xres = queuedFrame.Width,
                    yres = queuedFrame.Height,
                    FourCC = FourCC_type_e.FourCC_type_BGRA,
                    frame_rate_N = 30,
                    frame_rate_D = 1,
                    picture_aspect_ratio = (float)queuedFrame.Width / queuedFrame.Height,
                    frame_format_type = frame_format_type_e.frame_format_type_progressive,
                    timecode = NDIWrapper.send_timecode_synthesize,
                    p_data = pin.AddrOfPinnedObject(),
                    line_stride_in_bytes = queuedFrame.Stride,
                };

                NDIWrapper.send_send_video_v2(_sender, ref frame);
            }
            catch (Exception ex)
            {
                _frames.Writer.TryComplete(ex);
                SendFailed?.Invoke(ex);
                while (_frames.Reader.TryRead(out QueuedFrame pendingFrame))
                {
                    ArrayPool<byte>.Shared.Return(pendingFrame.Pixels);
                }
                return;
            }
            finally
            {
                pin.Free();
                ArrayPool<byte>.Shared.Return(queuedFrame.Pixels);
            }
        }
    }

    public void Dispose()
    {
        _frames.Writer.TryComplete();
        _sendTask.GetAwaiter().GetResult();
        if (_sender != nint.Zero)
        {
            NDIWrapper.send_destroy(_sender);
            _sender = nint.Zero;
        }
    }

    private readonly record struct QueuedFrame(byte[] Pixels, int Width, int Height, int Stride);
}
