using System;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using ZXing;

namespace RemoteTuning.Client.QRScanning
{
    /// <summary>
    /// Scans QR codes using the device camera and ZXing.Net.
    /// Suitable for Android/iOS.
    /// </summary>
    public class QRCodeScanner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int targetFPS = 30;
        [SerializeField] private int requestedWidth = 640;  // Reduced for better performance
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int decodeFrameInterval = 5; // Process every N frames (optimization)
        
        [Header("UI")]
        [SerializeField] private UnityEngine.UI.RawImage cameraPreview;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Status")]
        [SerializeField] private bool isScanning;
        [SerializeField] private string lastScannedData;
        
        private WebCamTexture _webcamTexture;
        private BarcodeReader _barcodeReader;
        private int _frameCount; // Process only every N frames
        
        public event Action<string> OnQRScanned;
        public bool IsScanning => isScanning;

        private void Start()
        {
            // Initialize ZXing reader
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };
        }

        /// <summary>
        /// Starts QR scanning using the device camera.
        /// </summary>
        public void StartScanning()
        {
            if (isScanning)
            {
                Debug.LogWarning("[QRScanner] Already scanning");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
#endif
            // Request camera permissions on Android/iOS
            #if UNITY_ANDROID || UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.Log("[QRScanner] Requesting camera permission...");
                UpdateStatus("Requesting camera permission...");
                StartCoroutine(RequestCameraPermission());
                return;
            }
            #endif

            // Verify cameras are available
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("[QRScanner] No cameras found!");
                UpdateStatus("No camera found\nCheck app permissions in Settings");
                return;
            }

            try
            {
                // Use back camera if available
                string deviceName = GetBackCamera();
                
                // Create WebCamTexture
                _webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, targetFPS);
                
                // Assign to UI
                if (cameraPreview != null)
                {
                    cameraPreview.texture = _webcamTexture;
                }
                
                // Start camera
                _webcamTexture.Play();
                isScanning = true;
                _frameCount = 0;
                
                // FixAspectRatio();
                
                UpdateStatus("Scanning...");
                Debug.Log($"[QRScanner] Camera started: {deviceName} ({_webcamTexture.width}x{_webcamTexture.height})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRScanner] Failed to start camera: {e.Message}");
                UpdateStatus($"Error: {e.Message}");
            }
        }

        /// <summary>
        /// Stops QR scanning and releases the camera.
        /// </summary>
        public void StopScanning()
        {
            if (!isScanning) return;

            if (_webcamTexture != null)
            {
                _webcamTexture.Stop();
                Destroy(_webcamTexture);
                _webcamTexture = null;
            }

            isScanning = false;
            UpdateStatus("Stopped");
            Debug.Log("[QRScanner] Camera stopped");
        }

        private System.Collections.IEnumerator RequestCameraPermission()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.Log("[QRScanner] Camera permission granted.");
                UpdateStatus("Permission granted");
                yield return new WaitForSeconds(0.5f);
                StartScanning();
            }
            else
            {
                Debug.LogError("[QRScanner] Camera permission DENIED.");
                UpdateStatus("Camera permission denied\nEnable in Settings -> Apps -> Permissions");
            }
        }

        private void Update()
        {
            if (!isScanning || _webcamTexture == null || !_webcamTexture.isPlaying)
                return;

            // Optimization: process only every N frames
            _frameCount++;
            if (_frameCount % decodeFrameInterval != 0)
                return;

            try
            {
                // Attempt to decode current frame
                var result = _barcodeReader.Decode(_webcamTexture.GetPixels32(), _webcamTexture.width, _webcamTexture.height);
                
                if (result != null)
                {
                    lastScannedData = result.Text;
                    Debug.Log($"[QRScanner] ══════════════════════════════════════");
                    Debug.Log($"[QRScanner] QR DETECTED");
                    Debug.Log($"[QRScanner] Data: {lastScannedData}");
                    Debug.Log($"[QRScanner] Data Length: {lastScannedData.Length}");
                    Debug.Log($"[QRScanner] ══════════════════════════════════════");
                    
                    if (OnQRScanned != null)
                    {
                        Debug.Log($"[QRScanner] Dispatching OnQRScanned event...");
                        OnQRScanned.Invoke(lastScannedData);
                        Debug.Log($"[QRScanner] OnQRScanned event dispatched.");
                    }
                    else
                    {
                        Debug.LogError($"[QRScanner] OnQRScanned is NULL - no subscribers!");
                        Debug.LogError($"[QRScanner] Make sure ClientConnector is active!");
                    }
                    
                    UpdateStatus($"Scanned!\n{lastScannedData.Substring(0, Math.Min(50, lastScannedData.Length))}...");
                    
                    Debug.Log($"[QRScanner] Stopping camera...");
                    // Stop scanning automatically after finding a QR
                    StopScanning();
                    Debug.Log($"[QRScanner] ══════════════════════════════════════");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRScanner] Error decoding: {e.Message}");
            }
        }

        private string GetBackCamera()
        {
            foreach (var device in WebCamTexture.devices)
            {
                // On Android/iOS, back camera is usually not "FrontFacing"
                if (!device.isFrontFacing)
                {
                    return device.name;
                }
            }
            
            // If no back camera, use first available
            return WebCamTexture.devices[0].name;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void FixAspectRatio()
        {
            if (cameraPreview == null || _webcamTexture == null)
                return;

            // Wait for camera to be ready
            if (_webcamTexture.width < 100)
            {
                Invoke(nameof(FixAspectRatio), 0.5f);
                return;
            }

            // Calculate camera aspect ratio
            float videoRatio = (float)_webcamTexture.width / _webcamTexture.height;
            
            // Get RectTransform
            RectTransform rt = cameraPreview.GetComponent<RectTransform>();
            if (rt == null) return;

            // Calculate container aspect ratio
            float containerRatio = rt.rect.width / rt.rect.height;

            // Adjust scale to maintain aspect ratio
            if (videoRatio > containerRatio)
            {
                // Video wider than container
                float scale = containerRatio / videoRatio;
                rt.localScale = new Vector3(1, scale, 1);
            }
            else
            {
                // Video taller than container
                float scale = videoRatio / containerRatio;
                rt.localScale = new Vector3(scale, 1, 1);
            }

            // Fix rotation on mobile devices
            // Camera feed may come rotated 90 or 270 degrees
            int rotation = -_webcamTexture.videoRotationAngle;
            
            // Apply rotation
            rt.localRotation = Quaternion.Euler(0, 0, rotation);

            // If rotated 90 or 270 degrees, swap width/height
            if (_webcamTexture.videoRotationAngle == 90 || _webcamTexture.videoRotationAngle == 270)
            {
                // Swap scale
                Vector3 scale = rt.localScale;
                rt.localScale = new Vector3(scale.y, scale.x, scale.z);
            }

            Debug.Log($"[QRScanner] Aspect ratio fixed: {_webcamTexture.width}x{_webcamTexture.height} (rotation: {rotation} deg)");
        }

        private void OnDestroy()
        {
            StopScanning();
        }

        // ═══════════════════════════════════════════════════════════
        // DEBUG & TESTING (Context Menu)
        // ═══════════════════════════════════════════════════════════
        
        [ContextMenu("Start Scanning")]
        private void StartScanningMenu()
        {
            StartScanning();
        }

        [ContextMenu("Stop Scanning")]
        private void StopScanningMenu()
        {
            StopScanning();
        }

        [ContextMenu("Debug Camera Info")]
        private void DebugCameraInfo()
        {
            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                Debug.Log($"═══ CAMERA INFO ═══");
                Debug.Log($"Device: {_webcamTexture.deviceName}");
                Debug.Log($"Resolution: {_webcamTexture.width}x{_webcamTexture.height}");
                Debug.Log($"Requested: {requestedWidth}x{requestedHeight}");
                Debug.Log($"Rotation: {_webcamTexture.videoRotationAngle}°");
                Debug.Log($"FPS: {_webcamTexture.requestedFPS} (requested: {targetFPS})");
                Debug.Log($"IsPlaying: {_webcamTexture.isPlaying}");
                Debug.Log($"Decode Interval: Every {decodeFrameInterval} frames");
                Debug.Log($"Current Frame: {_frameCount}");
            }
            else
            {
                Debug.LogWarning("Camera not running");
            }
        }

        [ContextMenu("List All Cameras")]
        private void ListAllCameras()
        {
            Debug.Log($"═══ AVAILABLE CAMERAS ({WebCamTexture.devices.Length}) ═══");
            for (int i = 0; i < WebCamTexture.devices.Length; i++)
            {
                var device = WebCamTexture.devices[i];
                Debug.Log($"[{i}] {device.name} - {(device.isFrontFacing ? "FRONT" : "BACK")}");
            }
        }
    }
}

