using System.IO;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    public static class ObserverSourceTextureLoader
    {
        public static Texture2D LoadColorKeyedJpeg(string relativeAssetPath, float threshold = 0.12f)
        {
            if (string.IsNullOrWhiteSpace(relativeAssetPath))
            {
                return null;
            }

            string fullPath = Path.Combine(Application.dataPath, relativeAssetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(bytes, false))
            {
                Object.Destroy(texture);
                return null;
            }

            Color key = texture.GetPixel(0, texture.height - 1);
            Color[] pixels = texture.GetPixels();
            float thresholdSqr = threshold * threshold;
            for (int index = 0; index < pixels.Length; ++index)
            {
                Color pixel = pixels[index];
                float dr = pixel.r - key.r;
                float dg = pixel.g - key.g;
                float db = pixel.b - key.b;
                float distanceSqr = (dr * dr) + (dg * dg) + (db * db);
                if (distanceSqr <= thresholdSqr)
                {
                    pixel.a = 0f;
                }
                else if (distanceSqr <= thresholdSqr * 2.5f)
                {
                    pixel.a = Mathf.Clamp01((distanceSqr - thresholdSqr) / (thresholdSqr * 1.5f));
                }
                else
                {
                    pixel.a = 1f;
                }

                pixels[index] = pixel;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            texture.name = Path.GetFileNameWithoutExtension(relativeAssetPath) + "_RuntimeKeyed";
            return texture;
        }
    }
}
