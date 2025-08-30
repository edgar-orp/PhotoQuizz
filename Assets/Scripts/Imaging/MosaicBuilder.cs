using System.Collections.Generic;
using UnityEngine;

namespace dev.xrlab.photoquizz.Imaging
{
    public class MosaicBuilder
    {
        private readonly PhotoLayoutOrientation _layout;

        // Constructor: por defecto Landscape
        public MosaicBuilder(PhotoLayoutOrientation layout = PhotoLayoutOrientation.Landscape)
        {
            _layout = layout;
        }

        // Dimensiones según orientación
        public int WIDTH => _layout == PhotoLayoutOrientation.Landscape ? 1800 : 1200;
        public int HEIGHT => _layout == PhotoLayoutOrientation.Landscape ? 1200 : 1800;

        public Texture2D Build(List<Texture2D> photos)
        {
            var canvas = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGBA32, false);
            var clear = new Color[WIDTH * HEIGHT];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.white;
            canvas.SetPixels(clear);

            int cw = WIDTH / 2;   // tile width
            int ch = HEIGHT / 2;  // tile height  -> en Portrait esto es 600x900 (aspect 2:3)

            for (int i = 0; i < 4; i++)
            {
                var src = (photos != null && i < photos.Count && photos[i] != null)
                    ? photos[i]
                    : (photos != null && photos.Count > 0 ? photos[0] : null);

                if (src == null) continue;

                // Recorta al aspect del tile antes de redimensionar (3:2 en Landscape, 2:3 en Portrait)
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