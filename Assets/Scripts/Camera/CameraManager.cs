using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private AspectRatioFitter aspectFitter; // opcional, no lo tocaremos

        [Header("Settings")]
        [SerializeField] private int width = 1080;
        [SerializeField] private int height = 1920;
        [SerializeField] private int fps = 30;
        [SerializeField] private int deviceIndex = 0;

        private WebCamTexture camTex;

        public void StartPreview()
        {
            if (previewImage == null) return;

            if (camTex == null)
            {
                var devices = WebCamTexture.devices;
                if (devices.Length > 0 && deviceIndex < devices.Length)
                    camTex = new WebCamTexture(devices[deviceIndex].name, width, height, fps);
                else
                    camTex = new WebCamTexture(width, height, fps);
            }

            previewImage.texture = camTex;
            camTex.Play();

            // NO forzar aspectFitter al aspecto de la cámara:
            // Queremos respetar el marco (previewImage) y recortar el feed para que "llene" sin deformar.

            ApplyPreviewTransformAndCrop(); // <-- NUEVO
        }

        public void StopPreview()
        {
            if (camTex != null && camTex.isPlaying)
                camTex.Stop();
        }

        // === NUEVO: rotar el RawImage según la cámara y recortar el feed como "cover" dentro del marco ===
        private void ApplyPreviewTransformAndCrop()
        {
            if (previewImage == null || camTex == null) return;

            // 1) Rotar visualmente el RawImage para que el feed se vea derecho
            int angle = camTex.videoRotationAngle; // 0, 90, 180, 270
            previewImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);

            // 2) Calcular aspecto del feed considerando la rotación
            int texW = camTex.width;
            int texH = camTex.height;
            bool rotated90 = (angle == 90 || angle == 270);
            if (rotated90)
            {
                // Si la cámara reporta 90/270, el "aspecto visible" se invierte
                int tmp = texW;
                texW = texH;
                texH = tmp;
            }
            float arTex = (texH == 0) ? 1f : (float)texW / texH;

            // 3) Aspecto del marco/máscara (previewImage)
            float arMask = GetPreviewAspect();

            // 4) Recorte tipo "cover" con uvRect (sin deformar):
            //    - Si el feed es más ancho que el marco, recortamos lados
            //    - Si es más alto, recortamos arriba/abajo
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            if (arTex > arMask)
            {
                // Demasiado ancho: recortar a los lados
                float visibleWidth = arMask / arTex;      // fracción [0..1]
                uv.x = (1f - visibleWidth) * 0.5f;
                uv.width = visibleWidth;
            }
            else if (arTex < arMask)
            {
                // Demasiado alto: recortar arriba/abajo
                float visibleHeight = arTex / arMask;     // fracción [0..1]
                uv.y = (1f - visibleHeight) * 0.5f;
                uv.height = visibleHeight;
            }

            // 5) Corregir espejo vertical si aplica
            if (camTex.videoVerticallyMirrored)
            {
                // Invertimos Y manteniendo el recorte centrado
                uv = new Rect(uv.x, 1f - uv.y, uv.width, -uv.height);
            }

            previewImage.uvRect = uv;
        }

        private float GetPreviewAspect()
        {
            if (previewImage == null) return 1f;
            var r = previewImage.rectTransform.rect;
            return (r.height <= 0f) ? 1f : (r.width / r.height);
        }

        // === ACTUALIZADO: recorta según la "máscara" del RawImage ===
        public Texture2D CapturePhoto()
        {
            if (camTex == null) return null;

            // Copiamos el frame actual de la cámara
            Texture2D fullFrame = new Texture2D(camTex.width, camTex.height, TextureFormat.RGBA32, false);
            fullFrame.SetPixels32(camTex.GetPixels32());
            fullFrame.Apply();

            // Usar el aspect del RawImage como máscara (ancho/alto de preview)
            float targetRatio = GetPreviewAspect();

            int cropWidth = fullFrame.width;
            int cropHeight = Mathf.RoundToInt(cropWidth / targetRatio);

            if (cropHeight > fullFrame.height)
            {
                // Ajustar al revés si no cabe en altura
                cropHeight = fullFrame.height;
                cropWidth = Mathf.RoundToInt(cropHeight * targetRatio);
            }

            // Centrar el recorte
            int startX = (fullFrame.width - cropWidth) / 2;
            int startY = (fullFrame.height - cropHeight) / 2;

            // Si la textura viene volteada verticalmente y quisieras
            // "emparejar" exactamente lo que ves, el uvRect ya lo corrige en pantalla.
            // Para el recorte de datos no necesitamos invertir, porque GetPixels lee crudo.
            // (Si en tu flujo ves la imagen rotada 90°, considera rotarla aquí según videoRotationAngle.)

            Color[] pixels = fullFrame.GetPixels(startX, startY, cropWidth, cropHeight);
            Texture2D cropped = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);
            cropped.SetPixels(pixels);
            cropped.Apply();

            return cropped;
        }
    }
}