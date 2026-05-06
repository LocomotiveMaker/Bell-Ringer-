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
(VideoCapture capture, VideoCaptureAPIs backend) = OpenCamera(options.CameraIndex, options.PreferredBackend);
using (capture)
{
    if (!capture.IsOpened())
    {
        Console.Error.WriteLine($"Failed to open camera index {options.CameraIndex}.");
        Console.Error.WriteLine("Run with --scan-cameras first and confirm the phone webcam index.");
        return;
    }

    ConfigureCapture(capture, options);

    using Mat frame = new Mat();
    using Mat gray = new Mat();
    using Mat previewFrame = new Mat();

    OpenCvSharp.Aruco.Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.DictArucoOriginal);
    DetectorParameters detectorParameters = new DetectorParameters();
    JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        IncludeFields = true
    };

    TrackerState trackerState = new TrackerState();
    RuntimeStats runtimeStats = new RuntimeStats();
    bool keepRunning = true;
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        keepRunning = false;
    };

    Stopwatch stopwatch = Stopwatch.StartNew();
    double processFpsEstimate = 0.0;
    double lastFrameTimeSeconds = stopwatch.Elapsed.TotalSeconds;
    double lastStatsPrintSeconds = -999.0;

    Console.WriteLine($"Camera index: {options.CameraIndex}");
    Console.WriteLine($"Selected backend: {backend}");
    Console.WriteLine($"Actual frame size: {capture.FrameWidth:0} x {capture.FrameHeight:0}");
    Console.WriteLine($"Reported driver FPS: {capture.Fps:0.##}");
    Console.WriteLine($"Detect max dimension: {options.DetectMaxDimension}px");
    Console.WriteLine($"Preview max dimension: {options.PreviewMaxDimension}px");
    Console.WriteLine($"UDP target: {options.UdpHost}:{options.UdpPort}");
    Console.WriteLine($"Marker IDs: {string.Join(",", options.TargetMarkerIds)}");
    Console.WriteLine($"Horizontal FOV guess: {options.HorizontalFovDegrees:0.0} degrees");
    Console.WriteLine("Press Q or Esc in the preview window to stop.");

    while (keepRunning)
    {
        if (!capture.Read(frame) || frame.Empty())
        {
            Thread.Sleep(2);
            continue;
        }

        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

        DetectionResult detection = DetectMarkers(gray, frame.Width, frame.Height, options, dictionary, detectorParameters, trackerState);
        trackerState.Update(detection);
        runtimeStats.Update(detection);

        double nowSeconds = stopwatch.Elapsed.TotalSeconds;
        double deltaSeconds = Math.Max(0.0001, nowSeconds - lastFrameTimeSeconds);
        lastFrameTimeSeconds = nowSeconds;
        processFpsEstimate = processFpsEstimate <= 0.0
            ? (1.0 / deltaSeconds)
            : Lerp(processFpsEstimate, 1.0 / deltaSeconds, 0.12);

        TrackingPacket packet = BuildPacket(options, detection, frame.Width, frame.Height, processFpsEstimate, (float)nowSeconds);
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet, jsonOptions));
        udpClient.Send(payload, payload.Length, udpEndPoint);

        if (nowSeconds - lastStatsPrintSeconds >= options.ConsoleStatsIntervalSeconds)
        {
            lastStatsPrintSeconds = nowSeconds;
            Console.WriteLine(
                $"proc {packet.fps:0.0} fps | detected {packet.detected} | mode {detection.ModeLabel} | ids {packet.markerIds} | size {packet.markerSizePx:0.0}px | conf {packet.confidence:0.00}");
        }

        if (options.Preview)
        {
            double previewScale = PreparePreviewFrame(frame, previewFrame, options);
            DrawOverlay(previewScale >= 0.999 ? frame : previewFrame, packet, detection, previewScale);
            Cv2.ImShow("BellRinger Pad Tracker", previewScale >= 0.999 ? frame : previewFrame);
            int key = Cv2.WaitKey(1);
            if (key == 27 || key == 'q' || key == 'Q')
            {
                keepRunning = false;
            }
        }
    }

    Cv2.DestroyAllWindows();
}

static void ConfigureCapture(VideoCapture capture, PadTrackerOptions options)
{
    capture.Set(VideoCaptureProperties.BufferSize, 1);
    capture.Set(VideoCaptureProperties.FrameWidth, options.Width);
    capture.Set(VideoCaptureProperties.FrameHeight, options.Height);
    capture.Set(VideoCaptureProperties.Fps, options.Fps);
    capture.Set(VideoCaptureProperties.ConvertRgb, 1);
}

static DetectionResult DetectMarkers(
    Mat gray,
    int frameWidth,
    int frameHeight,
    PadTrackerOptions options,
    OpenCvSharp.Aruco.Dictionary dictionary,
    DetectorParameters detectorParameters,
    TrackerState trackerState)
{
    if (trackerState.HasLock && trackerState.FramesSinceSeen <= options.RoiPersistenceFrames && trackerState.FramesSinceFullFrame < options.FullFrameRefreshFrames)
    {
        Rect roi = ExpandRect(trackerState.LastBounds, options.RoiExpandFactor, options.RoiMarginPixels, frameWidth, frameHeight);
        DetectionResult roiDetection = DetectMarkersInRegion(gray, frameWidth, frameHeight, options, dictionary, detectorParameters, roi, DetectionMode.Roi);
        if (roiDetection.Detected)
        {
            return roiDetection;
        }
    }

    return DetectMarkersInRegion(gray, frameWidth, frameHeight, options, dictionary, detectorParameters, null, DetectionMode.FullFrame);
}

static DetectionResult DetectMarkersInRegion(
    Mat gray,
    int frameWidth,
    int frameHeight,
    PadTrackerOptions options,
    OpenCvSharp.Aruco.Dictionary dictionary,
    DetectorParameters detectorParameters,
    Rect? region,
    DetectionMode mode)
{
    Rect searchRect = region ?? new Rect(0, 0, frameWidth, frameHeight);
    using Mat searchView = new Mat(gray, searchRect);

    double detectScale = ComputeScaleToMaxDimension(searchRect.Width, searchRect.Height, options.DetectMaxDimension);
    using Mat scaledSearch = new Mat();
    Mat detectView = searchView;

    if (detectScale < 0.999)
    {
        Size scaledSize = new Size(
            Math.Max(1, (int)Math.Round(searchRect.Width * detectScale)),
            Math.Max(1, (int)Math.Round(searchRect.Height * detectScale)));
        Cv2.Resize(searchView, scaledSearch, scaledSize, 0, 0, InterpolationFlags.Area);
        detectView = scaledSearch;
    }

    Point2f[][] corners;
    int[] ids;
    Point2f[][] rejected;
    CvAruco.DetectMarkers(detectView, dictionary, out corners, out ids, detectorParameters, out rejected);

    if (ids == null || corners == null || ids.Length == 0 || corners.Length == 0)
    {
        return DetectionResult.NotDetected(mode, searchRect, detectScale);
    }

    List<int> visibleIds = new List<int>();
    List<Point2f[]> mappedCorners = new List<Point2f[]>();
    for (int i = 0; i < ids.Length; i++)
    {
        if (!options.TargetMarkerIds.Contains(ids[i]))
        {
            continue;
        }

        Point2f[] markerCorners = corners[i];
        if (markerCorners.Length != 4)
        {
            continue;
        }

        Point2f[] restoredCorners = new Point2f[4];
        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
        {
            Point2f corner = markerCorners[cornerIndex];
            restoredCorners[cornerIndex] = new Point2f(
                (float)((corner.X / detectScale) + searchRect.X),
                (float)((corner.Y / detectScale) + searchRect.Y));
        }

        visibleIds.Add(ids[i]);
        mappedCorners.Add(restoredCorners);
    }

    if (visibleIds.Count == 0)
    {
        return DetectionResult.NotDetected(mode, searchRect, detectScale);
    }

    Rect bounds = ComputeBounds(mappedCorners);
    return DetectionResult.CreateDetected(visibleIds.ToArray(), mappedCorners.ToArray(), bounds, mode, searchRect, detectScale);
}

static double PreparePreviewFrame(Mat frame, Mat previewFrame, PadTrackerOptions options)
{
    double previewScale = ComputeScaleToMaxDimension(frame.Width, frame.Height, options.PreviewMaxDimension);
    if (previewScale >= 0.999)
    {
        return 1.0;
    }

    Size previewSize = new Size(
        Math.Max(1, (int)Math.Round(frame.Width * previewScale)),
        Math.Max(1, (int)Math.Round(frame.Height * previewScale)));
    Cv2.Resize(frame, previewFrame, previewSize, 0, 0, InterpolationFlags.Area);
    return previewScale;
}

static void ScanCameras(PadTrackerOptions options)
{
    for (int index = 0; index < options.ScanCount; index++)
    {
        (VideoCapture capture, VideoCaptureAPIs backend) = OpenCamera(index, options.PreferredBackend);
        using (capture)
        {
            bool opened = capture.IsOpened();
            if (!opened)
            {
                Console.WriteLine($"[{index}] unavailable");
                continue;
            }

            ConfigureCapture(capture, options);
            using Mat frame = new Mat();
            bool read = capture.Read(frame) && !frame.Empty();
            Console.WriteLine(read
                ? $"[{index}] OK {frame.Width}x{frame.Height} backend={backend} reported-fps={capture.Fps:0.##}"
                : $"[{index}] opened but no frame");
        }
    }
}

static (VideoCapture capture, VideoCaptureAPIs backend) OpenCamera(int index, string? preferredBackend = null)
{
    VideoCaptureAPIs[] backends = ResolveBackendOrder(preferredBackend);

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

static VideoCaptureAPIs[] ResolveBackendOrder(string? preferredBackend)
{
    if (!string.IsNullOrWhiteSpace(preferredBackend))
    {
        switch (preferredBackend.Trim().ToLowerInvariant())
        {
            case "dshow":
                return new[] { VideoCaptureAPIs.DSHOW };
            case "msmf":
                return new[] { VideoCaptureAPIs.MSMF };
            case "any":
                return new[] { VideoCaptureAPIs.ANY };
        }
    }

    return new[]
    {
        VideoCaptureAPIs.DSHOW,
        VideoCaptureAPIs.MSMF,
        VideoCaptureAPIs.ANY
    };
}

static TrackingPacket BuildPacket(
    PadTrackerOptions options,
    DetectionResult detection,
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

    if (!detection.Detected || detection.MarkerIds.Length == 0 || detection.MarkerCorners.Length == 0)
    {
        return packet;
    }

    double sumCenterX = 0.0;
    double sumCenterY = 0.0;
    double sumMarkerSize = 0.0;
    List<int> visibleIds = new List<int>();

    for (int i = 0; i < detection.MarkerIds.Length; i++)
    {
        Point2f[] markerCorners = detection.MarkerCorners[i];
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
        visibleIds.Add(detection.MarkerIds[i]);
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
    double scaleConfidence = detection.SearchScale < 0.999 ? 0.97 : 1.0;
    double confidence = Clamp01(((sizeConfidence * 0.55) + (idConfidence * 0.45)) * scaleConfidence);

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
    packet.mode = detection.ModeLabel;
    packet.searchScale = (float)detection.SearchScale;
    return packet;
}

static void DrawOverlay(Mat preview, TrackingPacket packet, DetectionResult detection, double previewScale)
{
    if (packet.detected)
    {
        for (int i = 0; i < detection.MarkerIds.Length; i++)
        {
            Point2f[] markerCorners = detection.MarkerCorners[i];
            Scalar lineColor = detection.MarkerIds[i] == 23 || detection.MarkerIds[i] == 47
                ? new Scalar(70, 240, 110)
                : new Scalar(80, 80, 80);

            for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                Point start = ToPoint(markerCorners[cornerIndex], previewScale);
                Point end = ToPoint(markerCorners[(cornerIndex + 1) % 4], previewScale);
                Cv2.Line(preview, start, end, lineColor, 2, LineTypes.AntiAlias);
            }

            Point labelPosition = ToPoint(markerCorners[0], previewScale);
            Cv2.PutText(preview, $"ID {detection.MarkerIds[i]}", new Point(labelPosition.X, labelPosition.Y - 8), HersheyFonts.HersheySimplex, 0.55, lineColor, 2, LineTypes.AntiAlias);
        }
    }

    Scalar infoColor = packet.detected ? new Scalar(50, 250, 220) : new Scalar(0, 180, 255);
    Cv2.PutText(preview, $"Detected: {packet.detected}  IDs: {packet.markerIds}", new Point(16, 30), HersheyFonts.HersheySimplex, 0.7, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(preview, $"X {packet.approxX:0.000}m  Y {packet.approxY:0.000}m  Z {packet.approxZ:0.000}m", new Point(16, 58), HersheyFonts.HersheySimplex, 0.65, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(preview, $"Screen {packet.screenX01:0.000}, {packet.screenY01:0.000}  size {packet.markerSizePx:0.0}px", new Point(16, 86), HersheyFonts.HersheySimplex, 0.65, infoColor, 2, LineTypes.AntiAlias);
    Cv2.PutText(preview, $"FPS {packet.fps:0.0}  mode {packet.mode}  scale {packet.searchScale:0.00}", new Point(16, 114), HersheyFonts.HersheySimplex, 0.65, new Scalar(230, 230, 230), 2, LineTypes.AntiAlias);

    if (packet.detected)
    {
        int centerX = (int)Math.Round(packet.screenX01 * preview.Width);
        int centerY = (int)Math.Round(packet.screenY01 * preview.Height);
        int radius = Math.Max(6, (int)Math.Round(Math.Clamp(packet.confidence, 0.2f, 1f) * 18f));
        Cv2.Circle(preview, new Point(centerX, centerY), radius, new Scalar(255, 255, 255), 2, LineTypes.AntiAlias);
        Cv2.Line(preview, new Point(centerX - 12, centerY), new Point(centerX + 12, centerY), new Scalar(255, 255, 255), 1, LineTypes.AntiAlias);
        Cv2.Line(preview, new Point(centerX, centerY - 12), new Point(centerX, centerY + 12), new Scalar(255, 255, 255), 1, LineTypes.AntiAlias);
    }
}

static Rect ExpandRect(Rect rect, double expandFactor, int marginPixels, int frameWidth, int frameHeight)
{
    double centerX = rect.X + (rect.Width * 0.5);
    double centerY = rect.Y + (rect.Height * 0.5);
    double halfWidth = (rect.Width * expandFactor * 0.5) + marginPixels;
    double halfHeight = (rect.Height * expandFactor * 0.5) + marginPixels;

    int x = Math.Max(0, (int)Math.Floor(centerX - halfWidth));
    int y = Math.Max(0, (int)Math.Floor(centerY - halfHeight));
    int right = Math.Min(frameWidth, (int)Math.Ceiling(centerX + halfWidth));
    int bottom = Math.Min(frameHeight, (int)Math.Ceiling(centerY + halfHeight));
    return new Rect(x, y, Math.Max(1, right - x), Math.Max(1, bottom - y));
}

static Rect ComputeBounds(IReadOnlyList<Point2f[]> markerCorners)
{
    float minX = float.MaxValue;
    float minY = float.MaxValue;
    float maxX = float.MinValue;
    float maxY = float.MinValue;

    foreach (Point2f[] corners in markerCorners)
    {
        foreach (Point2f corner in corners)
        {
            minX = Math.Min(minX, corner.X);
            minY = Math.Min(minY, corner.Y);
            maxX = Math.Max(maxX, corner.X);
            maxY = Math.Max(maxY, corner.Y);
        }
    }

    return new Rect(
        (int)Math.Floor(minX),
        (int)Math.Floor(minY),
        Math.Max(1, (int)Math.Ceiling(maxX - minX)),
        Math.Max(1, (int)Math.Ceiling(maxY - minY)));
}

static double ComputeScaleToMaxDimension(int width, int height, int maxDimension)
{
    int longestEdge = Math.Max(width, height);
    if (longestEdge <= 0 || longestEdge <= maxDimension)
    {
        return 1.0;
    }

    return maxDimension / (double)longestEdge;
}

static double Distance(Point2f a, Point2f b)
{
    double dx = a.X - b.X;
    double dy = a.Y - b.Y;
    return Math.Sqrt((dx * dx) + (dy * dy));
}

static Point ToPoint(Point2f point, double scale)
{
    return new Point((int)Math.Round(point.X * scale), (int)Math.Round(point.Y * scale));
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
    public string? PreferredBackend { get; private set; }
    public int DetectMaxDimension { get; private set; } = 720;
    public int PreviewMaxDimension { get; private set; } = 1280;
    public double RoiExpandFactor { get; private set; } = 2.6;
    public int RoiMarginPixels { get; private set; } = 48;
    public int RoiPersistenceFrames { get; private set; } = 4;
    public int FullFrameRefreshFrames { get; private set; } = 12;
    public double ConsoleStatsIntervalSeconds { get; private set; } = 1.0;
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
                case "--backend":
                    options.PreferredBackend = ReadString(args, ref i, options.PreferredBackend ?? string.Empty);
                    break;
                case "--detect-max-dim":
                    options.DetectMaxDimension = Math.Max(240, ReadInt(args, ref i, options.DetectMaxDimension));
                    break;
                case "--preview-max-dim":
                    options.PreviewMaxDimension = Math.Max(320, ReadInt(args, ref i, options.PreviewMaxDimension));
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
    public string mode = string.Empty;
    public float searchScale;
}

sealed class TrackerState
{
    public bool HasLock { get; private set; }
    public Rect LastBounds { get; private set; } = new Rect(0, 0, 1, 1);
    public int FramesSinceSeen { get; private set; } = 9999;
    public int FramesSinceFullFrame { get; private set; } = 9999;

    public void Update(DetectionResult detection)
    {
        if (!detection.Detected)
        {
            FramesSinceSeen++;
            FramesSinceFullFrame++;
            return;
        }

        HasLock = true;
        LastBounds = detection.Bounds;
        FramesSinceSeen = 0;
        FramesSinceFullFrame = detection.Mode == DetectionMode.FullFrame ? 0 : FramesSinceFullFrame + 1;
    }
}

sealed class RuntimeStats
{
    public int TotalFrames { get; private set; }
    public int DetectedFrames { get; private set; }
    public int RoiFrames { get; private set; }
    public int FullFrames { get; private set; }

    public void Update(DetectionResult detection)
    {
        TotalFrames++;
        if (detection.Detected)
        {
            DetectedFrames++;
        }

        if (detection.Mode == DetectionMode.Roi)
        {
            RoiFrames++;
        }
        else
        {
            FullFrames++;
        }
    }
}

readonly struct DetectionResult
{
    public static DetectionResult NotDetected(DetectionMode mode, Rect searchRect, double searchScale)
    {
        return new DetectionResult(false, Array.Empty<int>(), Array.Empty<Point2f[]>(), new Rect(0, 0, 1, 1), mode, searchRect, searchScale);
    }

    public static DetectionResult CreateDetected(int[] markerIds, Point2f[][] markerCorners, Rect bounds, DetectionMode mode, Rect searchRect, double searchScale)
    {
        return new DetectionResult(true, markerIds, markerCorners, bounds, mode, searchRect, searchScale);
    }

    private DetectionResult(
        bool detected,
        int[] markerIds,
        Point2f[][] markerCorners,
        Rect bounds,
        DetectionMode mode,
        Rect searchRect,
        double searchScale)
    {
        Detected = detected;
        MarkerIds = markerIds;
        MarkerCorners = markerCorners;
        Bounds = bounds;
        Mode = mode;
        SearchRect = searchRect;
        SearchScale = searchScale;
    }

    public bool Detected { get; }
    public int[] MarkerIds { get; }
    public Point2f[][] MarkerCorners { get; }
    public Rect Bounds { get; }
    public DetectionMode Mode { get; }
    public Rect SearchRect { get; }
    public double SearchScale { get; }
    public string ModeLabel => Mode == DetectionMode.Roi ? "ROI" : "FULL";
}

enum DetectionMode
{
    FullFrame = 0,
    Roi = 1
}
