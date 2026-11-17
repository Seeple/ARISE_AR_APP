using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>Receives one bounded JPEG per TCP connection, off the Unity thread.</summary>
internal sealed class GraspImageReceiver : IDisposable
{
    public const int MaxImageBytes = 512 * 1024;
    public const int MaxImageSide = 640;
    public const int MaxPendingFrames = 4;

    internal sealed class Frame
    {
        public Guid Id;
        public IPAddress Sender;
        public byte[] Jpeg;
        public long ReceivedAt;
        public bool HasExpired => (Stopwatch.GetTimestamp() - ReceivedAt) / (double)Stopwatch.Frequency > 2;
    }

    readonly TcpListener listener;
    readonly Queue<Frame> frames = new Queue<Frame>();
    volatile bool stopped;

    public GraspImageReceiver(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(MaxPendingFrames);
        Task.Run(Receive);
    }

    public bool TryTakeFrame(out Frame frame)
    {
        lock (frames)
        {
            frame = frames.Count == 0 ? null : frames.Dequeue();
            return frame != null;
        }
    }

    void Receive()
    {
        while (!stopped)
        {
            try
            {
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    client.ReceiveTimeout = 1500;
                    var sender = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    Frame frame = ReadFrame(client.GetStream(), sender);
                    if (frame == null) continue;
                    lock (frames)
                    {
                        if (stopped) return;
                        if (frames.Count == MaxPendingFrames) frames.Dequeue();
                        frames.Enqueue(frame);
                    }
                }
            }
            catch (IOException) { } // Short, incomplete or timed-out frame.
            catch (SocketException) when (!stopped) { }
            catch (SocketException) when (stopped) { return; }
            catch (ObjectDisposedException) when (stopped) { return; }
            catch (InvalidOperationException) when (stopped) { return; }
        }
    }

    static Frame ReadFrame(Stream stream, IPAddress sender)
    {
        // ARK1 + ASCII UUID (32 hexadecimal bytes) + uint32 big-endian JPEG length.
        var header = new byte[40];
        if (!ReadFully(stream, header) || header[0] != 'A' || header[1] != 'R' ||
            header[2] != 'K' || header[3] != '1' ||
            !Guid.TryParseExact(Encoding.ASCII.GetString(header, 4, 32), "N", out Guid id) || id == Guid.Empty)
            return null;

        int length = (header[36] << 24) | (header[37] << 16) | (header[38] << 8) | header[39];
        if (length < 4 || length > MaxImageBytes) return null;
        var jpeg = new byte[length];
        if (!ReadFully(stream, jpeg) || !HasSupportedDimensions(jpeg)) return null;
        return new Frame { Id = id, Sender = sender, Jpeg = jpeg, ReceivedAt = Stopwatch.GetTimestamp() };
    }

    static bool ReadFully(Stream stream, byte[] data)
    {
        long start = Stopwatch.GetTimestamp();
        for (int read = 0; read < data.Length;)
        {
            int count = stream.Read(data, read, data.Length - read);
            if (count == 0 || (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency > 1.5)
                return false;
            read += count;
        }
        return true;
    }

    internal static bool HasSupportedDimensions(byte[] jpeg)
    {
        if (jpeg.Length < 4 || jpeg[0] != 0xff || jpeg[1] != 0xd8 ||
            jpeg[jpeg.Length - 2] != 0xff || jpeg[jpeg.Length - 1] != 0xd9) return false;

        int offset = 2;
        while (offset + 3 < jpeg.Length && jpeg[offset++] == 0xff)
        {
            while (offset < jpeg.Length && jpeg[offset] == 0xff) offset++;
            if (offset + 2 >= jpeg.Length) return false;
            int marker = jpeg[offset++];
            if (marker == 0xda || marker == 0xd9) return false;
            int length = (jpeg[offset] << 8) | jpeg[offset + 1];
            if (length < 2 || offset + length > jpeg.Length) return false;
            if (marker == 0xc0 || marker == 0xc2)
            {
                if (length < 8 || jpeg[offset + 2] != 8) return false;
                int height = (jpeg[offset + 3] << 8) | jpeg[offset + 4];
                int width = (jpeg[offset + 5] << 8) | jpeg[offset + 6];
                return width > 0 && height > 0 && width <= MaxImageSide && height <= MaxImageSide;
            }
            offset += length;
        }
        return false;
    }

    public void Dispose()
    {
        stopped = true;
        listener.Stop();
        lock (frames) frames.Clear();
    }
}
