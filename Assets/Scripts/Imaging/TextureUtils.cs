using UnityEngine;

namespace dev.xrlab.photoquizz.Imaging
{
    public static class TextureUtils
    {
        // -------- Resize (ya lo teníamos) --------
        public static Texture2D Resize(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        public static void Paste(Texture2D dst, Texture2D src, int x, int y)
        {
            var px = src.GetPixels(0, 0, src.width, src.height);
            dst.SetPixels(x, y, src.width, src.height, px);
        }

        // -------- NUEVO: Corrige rotación/espejo de capturas --------
        public static Texture2D ApplyOrientation(Texture2D src, int rotationClockwise, bool mirrorVertical)
        {
            rotationClockwise = ((rotationClockwise % 360) + 360) % 360;

            Texture2D rotated = src;
            if (rotationClockwise == 90) rotated = Rotate90CW(src);
            else if (rotationClockwise == 180) rotated = Rotate180(src);
            else if (rotationClockwise == 270) rotated = Rotate270CW(src);

            if (mirrorVertical) rotated = FlipVertical(rotated);
            return rotated;
        }

        // Rotaciones (Color32, eficientes)
        public static Texture2D Rotate90CW(Texture2D src)
        {
            int w = src.width; int h = src.height;
            var srcPx = src.GetPixels32();
            var dst = new Texture2D(h, w, TextureFormat.RGBA32, false);
            var dstPx = new Color32[srcPx.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int srcIdx = y * w + x;
                    int dx = h - 1 - y;
                    int dy = x;
                    int dstIdx = dy * h + dx; // dstWidth = h
                    dstPx[dstIdx] = srcPx[srcIdx];
                }
            dst.SetPixels32(dstPx);
            dst.Apply(false, false);
            return dst;
        }

        public static Texture2D Rotate180(Texture2D src)
        {
            int w = src.width; int h = src.height;
            var srcPx = src.GetPixels32();
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var dstPx = new Color32[srcPx.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int srcIdx = y * w + x;
                    int dx = w - 1 - x;
                    int dy = h - 1 - y;
                    int dstIdx = dy * w + dx;
                    dstPx[dstIdx] = srcPx[srcIdx];
                }
            dst.SetPixels32(dstPx);
            dst.Apply(false, false);
            return dst;
        }

        public static Texture2D Rotate270CW(Texture2D src)
        {
            int w = src.width; int h = src.height;
            var srcPx = src.GetPixels32();
            var dst = new Texture2D(h, w, TextureFormat.RGBA32, false);
            var dstPx = new Color32[srcPx.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int srcIdx = y * w + x;
                    int dx = y;
                    int dy = w - 1 - x;
                    int dstIdx = dy * h + dx; // dstWidth = h
                    dstPx[dstIdx] = srcPx[srcIdx];
                }
            dst.SetPixels32(dstPx);
            dst.Apply(false, false);
            return dst;
        }

        public static Texture2D FlipVertical(Texture2D src)
        {
            int w = src.width; int h = src.height;
            var srcPx = src.GetPixels32();
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var dstPx = new Color32[srcPx.Length];

            for (int y = 0; y < h; y++)
            {
                int invY = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    int srcIdx = y * w + x;
                    int dstIdx = invY * w + x;
                    dstPx[dstIdx] = srcPx[srcIdx];
                }
            }
            dst.SetPixels32(dstPx);
            dst.Apply(false, false);
            return dst;
        }

        // -------- NUEVO: Crop centrado a un aspect ratio (p.ej. 3:2) --------
        public static Texture2D CropToAspect(Texture2D src, int targetW, int targetH)
        {
            float targetAspect = (float)targetW / targetH;
            float srcAspect = (float)src.width / src.height;

            int cw, ch, cx, cy;
            if (srcAspect > targetAspect)
            {
                // demasiado ancho → recorta lados
                ch = src.height;
                cw = Mathf.RoundToInt(ch * targetAspect);
                cx = (src.width - cw) / 2;
                cy = 0;
            }
            else
            {
                // demasiado alto → recorta arriba/abajo
                cw = src.width;
                ch = Mathf.RoundToInt(cw / targetAspect);
                cx = 0;
                cy = (src.height - ch) / 2;
            }

            var cropped = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            var px = src.GetPixels(cx, cy, cw, ch);
            cropped.SetPixels(0, 0, cw, ch, px);
            cropped.Apply(false, false);
            return cropped;
        }
    }
}
