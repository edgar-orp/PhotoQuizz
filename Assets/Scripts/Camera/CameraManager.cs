using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RawImage previewImage;

        [Header("Settings")]
        [SerializeField] private int Width = 1080;
        [SerializeField] private int Height = 1920;
        [SerializeField] private int Fps = 30;
        [SerializeField] private int DeviceIndex = 0;

        private WebCamTexture camTex;

        public void StartPreview()
        {
            if (previewImage == null) return;

            if (camTex == null)
            {
                var devices = WebCamTexture.devices;
                if (devices.Length > 0 && DeviceIndex < devices.Length)
                    camTex = new WebCamTexture(devices[DeviceIndex].name, Width, Height, Fps);
                else
                    camTex = new WebCamTexture(Width, Height, Fps);
            }

            previewImage.texture = camTex;
            camTex.Play();

            // Ajustar rotación y espejo en la UI
            previewImage.rectTransform.localEulerAngles = new Vector3(0, 0, -camTex.videoRotationAngle);
            previewImage.uvRect = camTex.videoVerticallyMirrored
                ? new Rect(1, 0, -1, 1)
                : new Rect(0, 0, 1, 1);
        }

        public void StopPreview()
        {
            if (camTex != null && camTex.isPlaying)
                camTex.Stop();
        }
    }
}