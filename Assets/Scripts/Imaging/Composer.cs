using UnityEngine;

namespace dev.xrlab.photoquizz.Imaging
{
    public class Composer
    {
        // Tolerancia para comparar aspect ratios y decidir si conviene rotar el overlay
        private const float ASPECT_TOL = 0.01f;

        /// <summary>
        /// Combina el mosaico con un overlay (PNG con alpha).
        /// - Si autoRotateOverlay está activo y el aspect del overlay no coincide con el canvas,
        ///   lo rota 90° cuando eso mejore el match.
        /// - Si fitOverlay está activo, ajusta el overlay al tamaño exacto del mosaico.
        /// </summary>
        public Texture2D Compose(
            Texture2D mosaic,
            Texture2D frameOverlay = null,
            bool fitOverlay = true,
            bool autoRotateOverlay = true)
        {
            if (mosaic == null) return null;

            int W = mosaic.width;
            int H = mosaic.height;

            // Final inicia con el mosaico como base
            var final = new Texture2D(W, H, TextureFormat.RGBA32, false);
            final.SetPixels(mosaic.GetPixels());

            if (frameOverlay != null)
            {
                var overlay = frameOverlay;

                // 1) Autorrotar overlay si su orientación no coincide y rotarlo 90° mejora el aspect
                if (autoRotateOverlay)
                {
                    float canvasAR = (float)W / H;
                    float overlayAR = (float)overlay.width / overlay.height;

                    float err = Mathf.Abs(canvasAR - overlayAR);
                    float errRot = Mathf.Abs(canvasAR - ((float)overlay.height / overlay.width));
                    if (errRot + ASPECT_TOL < err)
                    {
                        // Requiere que el asset sea Read/Write
                        overlay = TextureUtils.Rotate90CW(overlay);
                    }
                }

                // 2) Ajustar tamaño del overlay al canvas (si no coincide)
                if (fitOverlay && (overlay.width != W || overlay.height != H))
                {
                    overlay = TextureUtils.Resize(overlay, W, H);
                }

                // 3) Alpha blend (si overlay y canvas tienen el mismo número de píxeles)
                var basePx = final.GetPixels();
                var ovPx = overlay.GetPixels();

                if (ovPx.Length == basePx.Length)
                {
                    for (int i = 0; i < basePx.Length; i++)
                    {
                        float a = ovPx[i].a;
                        if (a <= 0f) continue;               // completamente transparente
                        if (a >= 1f) { basePx[i] = ovPx[i]; continue; } // opaque replace

                        basePx[i] = basePx[i] * (1f - a) + ovPx[i] * a; // blend
                    }
                    final.SetPixels(basePx);
                }
                else
                {
                    // Fallback defensivo: si por alguna razón difieren, forzamos un resize y reemplazo completo
                    var resized = TextureUtils.Resize(overlay, W, H);
                    final.SetPixels(resized.GetPixels());
                }
            }

            final.Apply(false, false);
            return final;
        }
    }
}
