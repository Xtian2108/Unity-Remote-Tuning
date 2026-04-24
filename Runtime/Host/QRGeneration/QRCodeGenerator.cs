using System;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace RemoteTuning.Host.QRGeneration
{
    /// <summary>
    /// Generates QR codes using ZXing.Net
    /// Converts ConnectionInfo JSON into a visual QR (Texture2D)
    /// </summary>
    public static class QRCodeGenerator
    {
        /// <summary>
        /// Generates a QR code from text (JSON)
        /// 
        /// WARNING: This method may cause duplication issues in some cases.
        /// It is recommended to use GenerateQRManual() instead.
        /// </summary>
        /// <param name="text">Text to encode (e.g., ConnectionInfo JSON)</param>
        /// <param name="width">QR width in pixels</param>
        /// <param name="height">QR height in pixels</param>
        /// <returns>Texture2D with the QR code</returns>
        [System.Obsolete("Use GenerateQRManual() instead to avoid duplication issues", false)]
        public static Texture2D GenerateQR(string text, int width = 256, int height = 256)
        {
            try
            {
                // Create ZXing writer
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = height,
                        Width = width,
                        Margin = 2, // Margin for better detection
                        CharacterSet = "UTF-8"
                    }
                };

                // Generate QR as Color32[]
                var pixels = writer.Write(text);

                // ✓ CHECK: Ensure the array has the correct size
                if (pixels == null || pixels.Length != width * height)
                {
                    Debug.LogError($"[QRCodeGenerator] ✗ Invalid pixel array size: {pixels?.Length} (expected {width * height})");
                    return CreateErrorTexture(width, height);
                }

                // Create Texture2D with correct format
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; // No filtering for sharp QR
                texture.wrapMode = TextureWrapMode.Clamp;
                
                // Apply pixels
                texture.SetPixels32(pixels);
                texture.Apply(false); // Do not generate mipmaps

                Debug.Log($"[QRCodeGenerator] QR generated ({width}x{height})");
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRCodeGenerator] ✗ Failed to generate QR: {e.Message}\n{e.StackTrace}");
                return CreateErrorTexture(width, height);
            }
        }

        /// <summary>
        /// Generates a QR from ConnectionInfo
        /// NOTE: Now uses GenerateQRManual to avoid duplication issues
        /// </summary>
        public static Texture2D GenerateConnectionQR(Server.ConnectionInfo connectionInfo, int size = 256)
        {
            if (connectionInfo == null)
            {
                Debug.LogError("[QRCodeGenerator] ConnectionInfo is null");
                return CreateErrorTexture(size, size);
            }

            string json = connectionInfo.ToJson();
            return GenerateQRManual(json, size);
        }

        /// <summary>
        /// Generates an improved QR using manual method (no duplication)
        /// </summary>
        public static Texture2D GenerateQRManual(string text, int size = 512)
        {
            try
            {
                // Create encoder
                var writer = new QRCodeWriter();
                var hints = new System.Collections.Generic.Dictionary<EncodeHintType, object>
                {
                    { EncodeHintType.CHARACTER_SET, "UTF-8" },
                    { EncodeHintType.MARGIN, 2 },
                    { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.M }
                };

                // Generate bit matrix
                var bitMatrix = writer.encode(text, BarcodeFormat.QR_CODE, size, size, hints);

                // Create texture manually from BitMatrix
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                Color32[] pixels = new Color32[size * size];
                Color32 black = new Color32(0, 0, 0, 255);
                Color32 white = new Color32(255, 255, 255, 255);

                // Convert BitMatrix to pixels
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Invert Y for Unity (origin bottom-left)
                        int pixelIndex = (size - 1 - y) * size + x;
                        pixels[pixelIndex] = bitMatrix[x, y] ? black : white;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false);

                Debug.Log($"[QRCodeGenerator] QR generated ({size}x{size})");
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRCodeGenerator] ✗ Manual generation failed: {e.Message}\n{e.StackTrace}");
                return CreateErrorTexture(size, size);
            }
        }

        /// <summary>
        /// Creates an error texture (red) when generation fails
        /// </summary>
        private static Texture2D CreateErrorTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            
            // Fill with red to indicate error
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Saves the QR as PNG (useful for debugging or export)
        /// </summary>
        public static void SaveQRAsPNG(Texture2D qrTexture, string filePath)
        {
            try
            {
                byte[] bytes = qrTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log($"[QRCodeGenerator] QR saved to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[QRCodeGenerator] ✗ Failed to save QR: {e.Message}");
            }
        }
    }
}
