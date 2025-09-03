using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private AspectRatioFitter aspectFitter;

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

            // Ajustar el aspect ratio del RawImage según la textura
            if (aspectFitter != null)
            {
                float ratio = (float)camTex.width / camTex.height;
                aspectFitter.aspectRatio = ratio;
            }

            // Corregir espejo (muchas cámaras frontales vienen volteadas)
            previewImage.uvRect = camTex.videoVerticallyMirrored
                ? new Rect(0f, 1f, 1f, -1f)
                : new Rect(0f, 0f, 1f, 1f);
        }

        public void StopPreview()
        {
            if (camTex != null && camTex.isPlaying)
                camTex.Stop();
        }

        public Texture2D CapturePhoto()
        {
            if (camTex == null) return null;

            // Copia completa del frame
            Texture2D fullFrame = new Texture2D(camTex.width, camTex.height, TextureFormat.RGBA32, false);
            fullFrame.SetPixels32(camTex.GetPixels32());
            fullFrame.Apply();

            // Queremos recortar a 9:16
            float targetRatio = 9f / 16f;
            int cropWidth = fullFrame.width;
            int cropHeight = Mathf.RoundToInt(cropWidth / targetRatio);

            if (cropHeight > fullFrame.height)
            {
                // si no cabe en altura, ajusta al revés
                cropHeight = fullFrame.height;
                cropWidth = Mathf.RoundToInt(cropHeight * targetRatio);
            }

            // Centrar el recorte
            int startX = (fullFrame.width - cropWidth) / 2;
            int startY = (fullFrame.height - cropHeight) / 2;

            Color[] pixels = fullFrame.GetPixels(startX, startY, cropWidth, cropHeight);
            Texture2D cropped = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);
            cropped.SetPixels(pixels);
            cropped.Apply();

            return cropped;
        }

    }
}