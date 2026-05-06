using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenCvSharp;
using OpenCvSharp.Aruco;

PadTrackerOptions options = PadTrackerOptions.Parse(args);

if (options.ScanCameras)
{
    ScanCameras(options);
    return;
}

using UdpClient udpClient = new UdpClient();
IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Parse(options.UdpHost), options.UdpPort);
(VideoCapture capture, VideoCaptureAPIs backend) = OpenCamera(options.CameraIndex);
using (capture)
{
if (!capture.IsOpened())
{
    Console.Error.WriteLine($"Failed to open camera index {options.CameraIndex}.");
    Console.Error.WriteLine("Run with --scan-cameras first and confirm the phone webcam index.");
    return;
}

capture.Set(VideoCaptureProperties.FrameWidth, options.Width);
capture.Set(VideoCaptureProperties.FrameHeight, options.Height);
capture.Set(VideoCaptureProperties.Fps, options.Fps);
capture.Set(VideoCaptureProperties.ConvertRgb, 1);

using Mat frame = new Mat();
using Mat gray = new Mat();
OpenCvSharp.Aruco.Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.DictArucoOriginal);
DetectorParameters detectorParameters = new DetectorParameters();
JsonSerializerOptions jsonOptions = new JsonSerializerOptions
{
    IncludeFields = true
};

bool keepRunning = true;
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    keepRunning = false;
};

Stopwatch stopwatch = Stopwatch.StartNew();
double fpsEstimate = 0.0;
double lastFrameTimeSeconds = stopwatch.Elapsed.TotalSeconds;

Console.WriteLine($"Camera index: {options.CameraIndex}");
Console.WriteLine($"Selected backend: {backend}");
Console.WriteLine($"Actual frame size: {capture.FrameWidth:0} x {capture.FrameHeight:0}");
Console.WriteLine($"UDP target: {options.UdpHost}:{options.UdpPort}");
Console.WriteLine($"Marker IDs: {string.Join(",", options.TargetMarkerIds)}");
Console.WriteLine($"Horizontal FOV guess: {options.HorizontalFovDegrees:0.0} degrees");
Console.WriteLine("Press Q or Esc in the preview window to stop.");

while (keepRunning)
{
    if (!capture.Read(frame) || frame.Empty())
    {
        Thread.Sleep(10);
        continue;
    }

    Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

    Point2f[][] corners;
    int[] ids;
    Point2f[][] rejected;
    CvAruco.DetectMarkers(gray, dictionary, out corners, out ids, detectorParameters, out rejected);

    int frameWidth = frame.Width;
    int frameHeight = frame.Height;
    double nowSeconds = stopwatch.Elapsed.TotalSeconds;
    double deltaSeconds = Math.Max(0.0001, nowSeconds - lastFrameTimeSeconds);
    lastFrameTimeSeconds = nowSeconds;
    fpsEstimate = fpsEstimate <= 0.0 ? (1.0 / deltaSeconds) : Lerp(fpsEstimate, 1.0 / deltaSeconds, 0.12);

    TrackingPacket packet = BuildPacket(options, ids, corners, frameWidth, frameHeight, fpsEstimate, (float)nowSeconds);
    byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet, jsonOptions));
    udpClient.Send(payload, payload.Length, udpEndPoint);

    if (options.Preview)
    {
        DrawOverlay(frame, packet, ids, corners);
        Cv2.ImShow("BellRinger Pad Tracker", frame);
        int key = Cv2.WaitKey(1);
        if (key == 27 || key == 'q' || key == 'Q')
        {
            keepRunning = false;
        }
    }
}

Cv2.DestroyAllWindows();
}

static void ScanCameras(PadTrackerOptions options)
{
    for (int index = 0; index < options.ScanCount; index++)
    {
        (VideoCapture capture, VideoCaptureAPIs backend) = OpenCamera(index);
        using (capture)
        {
        bool opened = capture.IsOpened();
        if (!opened)
        {
            Console.WriteLine($"[{index}] unavailable");
            continue;
        }

        capture.Set(VideoCaptureProperties.FrameWidth, options.Width);
        capture.Set(VideoCaptureProperties.FrameHeight, options.Height);
        using Mat frame = new Mat();
        bool read = capture.Read(frame) && !frame.Empty();
        Console.WriteLine(read
            ? $"[{index}] OK {frame.Width}x{frame.Height} backend={backend}"
            : $"[{index}] opened but no frame");
        }
    }
}

static (VideoCapture capture, VideoCaptureAPIs backend) OpenCamera(int index)
{
    VideoCaptureAPIs[] backends =
    {
        VideoCaptureAPIs.DSHOW,
        VideoCaptureAPIs.MSMF,
        VideoCaptureAPIs.ANY
    };

    foreach (VideoCaptureAPIs backend in backends)
    {
        VideoCapture capture = new VideoCapture(index, backend);
        if (capture.IsOpened())
        {
            return (capture, backend);
        }

        capture.Dispose();
    }

    return (new VideoCapture(), VideoCaptureAPIs.ANY);
}

static TrackingPacket BuildPacket(
    PadTrackerOptions options,
    int[]? ids,
    Point2f[][]? corners,
    int frameWidth,
    int frameHeight,
    double fpsEstimate,
    float nowSeconds)
{
    TrackingPacket packet = new TrackingPacket
    {
        detected = false,
        markerCount = 0,
        markerIds = string.Empty,
        frameWidth = frameWidth,
        frameHeight = frameHeight,
        fps = (float)fpsEstimate,
        screenX01 = 0.5f,
        screenY01 = 0.5f,
        approxX = 0f,
        approxY = 0f,
        approxZ = 0f,
        markerSizePx = 0f,
        confidence = 0f,
        timeSeconds = nowSeconds
    };

    if (ids == null || corners == null || ids.Length == 0 || corners.Length == 0)
    {
        return packet;
    }

    double sumCenterX = 0.0;
    double sumCenterY = 0.0;
    double sumMarkerSize = 0.0;
    List<int> visibleIds = new List<int>();

    for (int i = 0; i < ids.Length; i++)
    {
        int id = ids[i];
        if (!options.TargetMarkerIds.Contains(id))
        {
            continue;
        }

        Point2f[] markerCorners = corners[i];
        if (markerCorners.Length != 4)
        {
            continue;
        }

        double centerX = 0.0;
        double centerY = 0.0;
        double edgeSize = 0.0;
        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
        {
            Point2f corner = markerCorners[cornerIndex];
            centerX += corner.X;
            centerY += corner.Y;

            Point2f nextCorner = markerCorners[(cornerIndex + 1) % 4];
            edgeSize += Distance(corner, nextCorner);
        }

        centerX *= 0.25;
        centerY *= 0.25;
        edgeSize *= 0.25;

        sumCenterX += centerX;
        sumCenterY += centerY;
        sumMarkerSize += edgeSize;
        visibleIds.Add(id);
    }

    if (visibleIds.Count == 0)
    {
        return packet;
    }

    double averageCenterX = sumCenterX / visibleIds.Count;
    double averageCenterY = sumCenterY / visibleIds.Count;
    double averageMarkerSize = Math.Max(1.0, sumMarkerSize / visibleIds.Count);
    double focalPixels = frameWidth / (2.0 * Math.Tan(options.HorizontalFovDegrees * Math.PI / 360.0));
    double approxZ = (options.MarkerSizeMeters * focalPixels) / averageMarkerSize;
    double approxX = ((averageCenterX - (frameWidth * 0.5)) / focalPixels) * approxZ;
    double approxY = (((frameHeight * 0.5) - averageCenterY) / focalPixels) * approxZ;
    double sizeConfidence = Clamp01(averageMarkerSize / 120.0);
    double idConfidence = visibleIds.Count >= 2 ? 1.0 : 0.68;
    double confidence = Clamp01((sizeConfidence * 0.55) + (idConfidence * 0.45));

    packet.detected = true;
    packet.markerCount = visibleIds.Count;
    packet.markerIds = string.Join(",", visibleIds);
    packet.screenX01 = (float)(averageCenterX / frameWidth);
    packet.screenY01 = (float)(averageCenterY / frameHeight);
    packet.approxX = (float)approxX;
    packet.approxY = (float)approxY;
    packet.approxZ = (float)approxZ;
    packet.markerSizePx = (float)averageMarkerSize;
    packet.confidence = (float)confidence;
    return packet;
}

static void DrawOverlay(Mat frame, TrackingPacket packet, int[]? ids, Point2f[][]? corners)
{
    if (ids != null && corners != null)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            Point2f[] markerCorners = corners[i];
            if (markerCorners.Length != 4)
            {
                continue;
            }

            Scalar lineColor = ids[i] == 23 || ids[i] == 47
                ? new Scalar(70, 240, 110)
                : new Scalar(80, 80, 80);

            for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                Point start = ToPoint(markerCorners[cornerIndex]);
                Point end = ToPoint(markerCorners[(cornerIndex + 1) % 4]);
                Cv2.Line(frame, start, end, lineColor, 2, LineTypes.AntiAlias);
            }

            Point labelPosition = ToPoint(markerCorners[0]);
            Cv2.PutText(frame, $"ID {ids[i]}", new Point(labelPosition.X, labelPosition.Y - 8), HersheyFonts.HersheySimplex, 0.55, lineColor, 2, LineTypes.AntiAlias);
        }
    }

    Scalar infoColor = packet.detected ? new Scalar(50, 250, 220) : new Scalar(0, 180, 255);
    Cv2.PutText(frame, $"Detected: {packet.detected}  IDs: {packet.markerIds}", new Point(16, 30), HersheyFonts.HersheySimplex, 0.7, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(frame, $"X {packet.approxX:0.000}m  Y {packet.approxY:0.000}m  Z {packet.approxZ:0.000}m", new Point(16, 58), HersheyFonts.HersheySimplex, 0.65, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(frame, $"Screen {packet.screenX01:0.000}, {packet.screenY01:0.000}  size {packet.markerSizePx:0.0}px", new Point(16, 86), HersheyFonts.HersheySimplex, 0.65, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(frame, $"FPS {packet.fps:0.0}  Q/Esc to quit", new Point(16, 114), HersheyFonts.HersheySimplex, 0.65, new Scalar(230, 230, 230), 2, LineTypes.AntiAlias);

    if (packet.detected)
    {
        int centerX = (int)Math.Round(packet.screenX01 * frame.Width);
        int centerY = (int)Math.Round(packet.screenY01 * frame.Height);
        int radius = Math.Max(6, (int)Math.Round(Math.Clamp(packet.confidence, 0.2f, 1f) * 18f));
        Cv2.Circle(frame, new Point(centerX, centerY), radius, new Scalar(255, 255, 255), 2, LineTypes.AntiAlias);
        Cv2.Line(frame, new Point(centerX - 12, centerY), new Point(centerX + 12, centerY), new Scalar(255, 255, 255), 1, LineTypes.AntiAlias);
        Cv2.Line(frame, new Point(centerX, centerY - 12), new Point(centerX, centerY + 12), new Scalar(255, 255, 255), 1, LineTypes.AntiAlias);
    }
}

static double Distance(Point2f a, Point2f b)
{
    double dx = a.X - b.X;
    double dy = a.Y - b.Y;
    return Math.Sqrt((dx * dx) + (dy * dy));
}

static Point ToPoint(Point2f point)
{
    return new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
}

static double Clamp01(double value)
{
    return Math.Max(0.0, Math.Min(1.0, value));
}

static double Lerp(double a, double b, double t)
{
    return a + ((b - a) * t);
}

sealed class PadTrackerOptions
{
    public int CameraIndex { get; private set; } = 0;
    public int Width { get; private set; } = 1280;
    public int Height { get; private set; } = 720;
    public int Fps { get; private set; } = 60;
    public string UdpHost { get; private set; } = "127.0.0.1";
    public int UdpPort { get; private set; } = 39051;
    public double HorizontalFovDegrees { get; private set; } = 68.0;
    public double MarkerSizeMeters { get; private set; } = 0.05;
    public bool Preview { get; private set; } = true;
    public bool ScanCameras { get; private set; }
    public int ScanCount { get; private set; } = 8;
    public HashSet<int> TargetMarkerIds { get; } = new HashSet<int> { 23, 47 };

    public static PadTrackerOptions Parse(string[] args)
    {
        PadTrackerOptions options = new PadTrackerOptions();

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            switch (argument)
            {
                case "--scan-cameras":
                    options.ScanCameras = true;
                    break;
                case "--no-preview":
                    options.Preview = false;
                    break;
                case "--camera-index":
                    options.CameraIndex = ReadInt(args, ref i, options.CameraIndex);
                    break;
                case "--width":
                    options.Width = ReadInt(args, ref i, options.Width);
                    break;
                case "--height":
                    options.Height = ReadInt(args, ref i, options.Height);
                    break;
                case "--fps":
                    options.Fps = ReadInt(args, ref i, options.Fps);
                    break;
                case "--udp-host":
                    options.UdpHost = ReadString(args, ref i, options.UdpHost);
                    break;
                case "--udp-port":
                    options.UdpPort = ReadInt(args, ref i, options.UdpPort);
                    break;
                case "--horizontal-fov-deg":
                    options.HorizontalFovDegrees = ReadDouble(args, ref i, options.HorizontalFovDegrees);
                    break;
                case "--marker-size-mm":
                    options.MarkerSizeMeters = ReadDouble(args, ref i, options.MarkerSizeMeters * 1000.0) / 1000.0;
                    break;
                case "--scan-count":
                    options.ScanCount = ReadInt(args, ref i, options.ScanCount);
                    break;
            }
        }

        return options;
    }

    private static int ReadInt(string[] args, ref int index, int fallback)
    {
        return index + 1 < args.Length && int.TryParse(args[index + 1], out int value)
            ? Advance(ref index, value)
            : fallback;
    }

    private static double ReadDouble(string[] args, ref int index, double fallback)
    {
        return index + 1 < args.Length && double.TryParse(args[index + 1], out double value)
            ? Advance(ref index, value)
            : fallback;
    }

    private static string ReadString(string[] args, ref int index, string fallback)
    {
        return index + 1 < args.Length
            ? Advance(ref index, args[index + 1])
            : fallback;
    }

    private static T Advance<T>(ref int index, T value)
    {
        index++;
        return value;
    }
}

sealed class TrackingPacket
{
    public bool detected;
    public int markerCount;
    public string markerIds = string.Empty;
    public int frameWidth;
    public int frameHeight;
    public float fps;
    public float screenX01;
    public float screenY01;
    public float approxX;
    public float approxY;
    public float approxZ;
    public float markerSizePx;
    public float confidence;
    public float timeSeconds;
}
