using System.Collections.Generic;
using UnityEngine;

namespace dev.xrlab.photoquizz.Imaging
{
    public class MosaicBuilder
    {
        // 6x4" @ 300dpi → 1800x1200 (si prefieres retrato 4x6, invierte estas constantes)
        public const int WIDTH = 1800;
        public const int HEIGHT = 1200;

        public Texture2D Build(List<Texture2D> photos)
        {
            var canvas = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
            var clear = new Color[WIDTH * HEIGHT];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.white;
            canvas.SetPixels(clear);

            int cw = WIDTH / 2;   // 900
            int ch = HEIGHT / 2;  // 600

            for (int i = 0; i < 4; i++)
            {
                var src = (photos != null && i < photos.Count && photos[i] != null)
                    ? photos[i]
                    : (photos != null && photos.Count > 0 ? photos[0] : null);

                if (src == null) continue;

                // >>> NUEVO: recorta a 3:2 antes de escalar, para evitar deformar
                var cropped = TextureUtils.CropToAspect(src, cw, ch);
                var resized = TextureUtils.Resize(cropped, cw, ch);

                int col = (i % 2);
                int row = (i / 2);
                int px = col * cw;
                int py = (1 - row) * ch; // origen abajo-izquierda

                TextureUtils.Paste(canvas, resized, px, py);
            }

            canvas.Apply();
            return canvas;
        }
    }
}
