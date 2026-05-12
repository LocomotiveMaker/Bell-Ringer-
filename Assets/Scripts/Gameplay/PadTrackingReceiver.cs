using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PadTrackingReceiver : MonoBehaviour
    {
        [SerializeField] private int listenPort = 39051;
        [SerializeField] private float staleAfterSeconds = 0.35f;
        [SerializeField] private float smoothingStrength = 14f;
        [SerializeField] private float cameraYawSmoothingStrength = 30f;
        [SerializeField] private float cameraPitchRollSmoothingStrength = 18f;

        private readonly object _packetLock = new object();
        private UdpClient _udpClient;
        private Thread _receiveThread;
        private bool _running;
        private bool _hasPendingPacket;
        private string _pendingJson;
        private string _lastError = string.Empty;
        private bool _detected;
        private bool _hasPose;
        private int _markerCount;
        private int _frameWidth;
        private int _frameHeight;
        private float _fps;
        private float _screenX01 = 0.5f;
        private float _screenY01 = 0.5f;
        private float _markerSizePx;
        private float _confidence;
        private float _lastPacketRealtime;
        private float _lastDetectionRealtime;
        private float _lastCameraYawRealtime;
        private float _lastCameraPitchRollRealtime;
        private string _markerIds = string.Empty;
        private string _lastPacketJson = string.Empty;
        private Vector3 _cameraSpacePosition = new Vector3(0f, 0f, 0.7f);
        private bool _cameraYawAvailable;
        private float _cameraYawDegrees;
        private bool _cameraPitchRollAvailable;
        private float _cameraPitchDegrees;
        private float _cameraRollDegrees;

        public int ListenPort => listenPort;
        public bool IsDetected => _detected;
        public bool HasPose => _hasPose;
        public bool HasFreshPacket => LastPacketAgeSeconds <= staleAfterSeconds;
        public bool HasFreshDetection => _detected && LastDetectionAgeSeconds <= staleAfterSeconds;
        public bool HasFreshCameraYaw => _cameraYawAvailable && LastCameraYawAgeSeconds <= staleAfterSeconds;
        public bool HasFreshCameraPitchRoll => _cameraPitchRollAvailable && LastCameraPitchRollAgeSeconds <= staleAfterSeconds;
        public int MarkerCount => _markerCount;
        public int FrameWidth => _frameWidth;
        public int FrameHeight => _frameHeight;
        public float FramesPerSecond => _fps;
        public float ScreenX01 => _screenX01;
        public float ScreenY01 => _screenY01;
        public float MarkerSizePixels => _markerSizePx;
        public float Confidence => _confidence;
        public float LastPacketAgeSeconds => _lastPacketRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastPacketRealtime;
        public float LastDetectionAgeSeconds => _lastDetectionRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastDetectionRealtime;
        public float LastCameraYawAgeSeconds => _lastCameraYawRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastCameraYawRealtime;
        public float LastCameraPitchRollAgeSeconds => _lastCameraPitchRollRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastCameraPitchRollRealtime;
        public string MarkerIds => _markerIds;
        public string LastPacketJson => _lastPacketJson;
        public string LastError => _lastError;
        public Vector3 ApproximateCameraSpacePosition => _cameraSpacePosition;
        public bool CameraYawAvailable => _cameraYawAvailable;
        public float CameraYawDegrees => _cameraYawDegrees;
        public bool CameraPitchRollAvailable => _cameraPitchRollAvailable;
        public float CameraPitchDegrees => _cameraPitchDegrees;
        public float CameraRollDegrees => _cameraRollDegrees;

        private void OnEnable()
        {
            StartReceiver();
        }

        private void OnDisable()
        {
            StopReceiver();
        }

        private void Update()
        {
            ConsumePendingPacket();
            if (_detected && LastDetectionAgeSeconds > staleAfterSeconds)
            {
                _detected = false;
            }

            if (_cameraYawAvailable && LastCameraYawAgeSeconds > staleAfterSeconds)
            {
                _cameraYawAvailable = false;
            }

            if (_cameraPitchRollAvailable && LastCameraPitchRollAgeSeconds > staleAfterSeconds)
            {
                _cameraPitchRollAvailable = false;
            }
        }

        private void OnValidate()
        {
            listenPort = Mathf.Clamp(listenPort, 1024, 65535);
            staleAfterSeconds = Mathf.Max(0.05f, staleAfterSeconds);
            smoothingStrength = Mathf.Max(0f, smoothingStrength);
            cameraYawSmoothingStrength = Mathf.Max(0f, cameraYawSmoothingStrength);
            cameraPitchRollSmoothingStrength = Mathf.Max(0f, cameraPitchRollSmoothingStrength);
        }

        private void StartReceiver()
        {
            if (_running)
            {
                return;
            }

            _lastError = string.Empty;
            _running = true;
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "PadTrackingReceiver"
            };
            _receiveThread.Start();
        }

        private void StopReceiver()
        {
            _running = false;

            try
            {
                _udpClient?.Close();
            }
            catch
            {
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(250);
            }

            _receiveThread = null;
            _udpClient = null;
        }

        private void ReceiveLoop()
        {
            try
            {
                _udpClient = new UdpClient(listenPort);
                _udpClient.Client.ReceiveTimeout = 250;
            }
            catch (Exception exception)
            {
                _lastError = $"UDP bind failed on {listenPort}: {exception.Message}";
                _running = false;
                return;
            }

            while (_running)
            {
                try
                {
                    System.Net.IPEndPoint remoteEndPoint = null;
                    byte[] payload = _udpClient.Receive(ref remoteEndPoint);
                    string json = Encoding.UTF8.GetString(payload);

                    lock (_packetLock)
                    {
                        _pendingJson = json;
                        _hasPendingPacket = true;
                    }
                }
                catch (SocketException socketException)
                {
                    if (socketException.SocketErrorCode == SocketError.TimedOut || socketException.SocketErrorCode == SocketError.Interrupted)
                    {
                        continue;
                    }

                    if (_running)
                    {
                        _lastError = $"UDP receive failed: {socketException.Message}";
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (_running)
                    {
                        _lastError = $"Receiver exception: {exception.Message}";
                    }
                }
            }
        }

        private void ConsumePendingPacket()
        {
            string json = null;
            lock (_packetLock)
            {
                if (!_hasPendingPacket)
                {
                    return;
                }

                json = _pendingJson;
                _pendingJson = null;
                _hasPendingPacket = false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            PadTrackingPacket packet = JsonUtility.FromJson<PadTrackingPacket>(json);
            if (packet == null)
            {
                return;
            }

            _lastPacketRealtime = Time.realtimeSinceStartup;
            _lastPacketJson = json;
            _markerCount = packet.markerCount;
            _markerIds = packet.markerIds ?? string.Empty;
            _frameWidth = packet.frameWidth;
            _frameHeight = packet.frameHeight;
            _fps = packet.fps;
            _screenX01 = packet.screenX01;
            _screenY01 = packet.screenY01;
            _markerSizePx = packet.markerSizePx;
            _confidence = packet.confidence;
            _detected = packet.detected;

            if (!packet.detected)
            {
                return;
            }

            _lastDetectionRealtime = Time.realtimeSinceStartup;
            Vector3 targetPosition = new Vector3(packet.approxX, packet.approxY, packet.approxZ);
            float lerpFactor = 1f - Mathf.Exp(-smoothingStrength * Time.unscaledDeltaTime);
            if (!_hasPose || smoothingStrength <= 0f)
            {
                _cameraSpacePosition = targetPosition;
                _hasPose = true;
            }
            else
            {
                _cameraSpacePosition = Vector3.Lerp(_cameraSpacePosition, targetPosition, lerpFactor);
            }

            if (packet.cameraYawAvailable)
            {
                bool hadCameraYawBefore = HasFreshCameraYaw;
                _lastCameraYawRealtime = Time.realtimeSinceStartup;
                float yawLerpFactor = 1f - Mathf.Exp(-cameraYawSmoothingStrength * Time.unscaledDeltaTime);
                if (!hadCameraYawBefore || cameraYawSmoothingStrength <= 0f)
                {
                    _cameraYawDegrees = packet.cameraYawDegrees;
                }
                else
                {
                    _cameraYawDegrees = Mathf.LerpAngle(_cameraYawDegrees, packet.cameraYawDegrees, yawLerpFactor);
                }

                _cameraYawAvailable = true;
            }

            if (packet.cameraPitchRollAvailable)
            {
                bool hadCameraPitchRollBefore = HasFreshCameraPitchRoll;
                _lastCameraPitchRollRealtime = Time.realtimeSinceStartup;
                float pitchRollLerpFactor = 1f - Mathf.Exp(-cameraPitchRollSmoothingStrength * Time.unscaledDeltaTime);
                if (!hadCameraPitchRollBefore || cameraPitchRollSmoothingStrength <= 0f)
                {
                    _cameraPitchDegrees = packet.cameraPitchDegrees;
                    _cameraRollDegrees = packet.cameraRollDegrees;
                }
                else
                {
                    _cameraPitchDegrees = Mathf.LerpAngle(_cameraPitchDegrees, packet.cameraPitchDegrees, pitchRollLerpFactor);
                    _cameraRollDegrees = Mathf.LerpAngle(_cameraRollDegrees, packet.cameraRollDegrees, pitchRollLerpFactor);
                }

                _cameraPitchRollAvailable = true;
            }
        }

        [Serializable]
        private sealed class PadTrackingPacket
        {
            public bool detected;
            public int markerCount;
            public string markerIds;
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
            public bool cameraYawAvailable;
            public float cameraYawDegrees;
            public bool cameraPitchRollAvailable;
            public float cameraPitchDegrees;
            public float cameraRollDegrees;
            public float timeSeconds;
        }
    }
}
