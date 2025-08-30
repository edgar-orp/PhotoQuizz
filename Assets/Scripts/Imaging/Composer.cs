using UnityEngine;

namespace dev.xrlab.photoquizz.Imaging
{
    public class Composer
    {
        // Sobrepone frameOverlay (1800x1200 con alpha) sobre el mosaico
        public Texture2D Compose(Texture2D mosaic, Texture2D frameOverlay = null)
        {
            if (mosaic == null) return null;

            var final = new Texture2D(MosaicBuilder.WIDTH, MosaicBuilder.HEIGHT, TextureFormat.RGBA32, false);
            final.SetPixels(mosaic.GetPixels());

            if (frameOverlay != null &&
                frameOverlay.width == MosaicBuilder.WIDTH &&
                frameOverlay.height == MosaicBuilder.HEIGHT)
            {
                var basePx = final.GetPixels();
                var ovPx = frameOverlay.GetPixels();
                for (int i = 0; i < basePx.Length; i++)
                {
                    float a = ovPx[i].a;
                    basePx[i] = basePx[i] * (1f - a) + ovPx[i] * a;
                }
                final.SetPixels(basePx);
            }

            final.Apply();
            return final;
        }
    }
}
