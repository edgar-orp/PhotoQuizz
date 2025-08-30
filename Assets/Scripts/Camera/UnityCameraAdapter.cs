using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Camera
{
    public class UnityCameraAdapter : MonoBehaviour, ICameraAdapter
    {
        private WebCamTexture _camTex;
        private Texture2D _lastFrame;
        private RawImage _target;

        public void StartPreview(RawImage target)
        {
            _target = target;

            if (_camTex == null)
            {
                var devices = WebCamTexture.devices;
                string name = devices.Length > 0 ? devices[0].name : null;
                _camTex = string.IsNullOrEmpty(name)
                    ? new WebCamTexture(1920, 1080, 30)
                    : new WebCamTexture(name, 1920, 1080, 30);
            }

            _camTex.Play();
            if (_target) _target.texture = _camTex;

            // Ajustar orientación del preview en el siguiente frame
            StartCoroutine(AdjustPreviewOrientationNextFrame());
        }

        public void StopPreview()
        {
            if (_camTex != null && _camTex.isPlaying) _camTex.Stop();
        }

        public IEnumerator CapturePhoto(System.Action<Texture2D> onDone)
        {
            // Asegura capturar el último buffer tras el render
            yield return new WaitForEndOfFrame();

            if (_camTex == null)
            {
                onDone?.Invoke(null);
                yield break;
            }

            if (_lastFrame == null || _lastFrame.width != _camTex.width || _lastFrame.height != _camTex.height)
                _lastFrame = new Texture2D(_camTex.width, _camTex.height, TextureFormat.RGBA32, false);

            _lastFrame.SetPixels32(_camTex.GetPixels32());
            _lastFrame.Apply(false, false);

            // Corrige orientación y espejo en los PIXELES capturados
            int angleCW = _camTex.videoRotationAngle;              // 0, 90, 180, 270 (clockwise)
            bool mirrorV = _camTex.videoVerticallyMirrored;        // espejado vertical
            var oriented = Imaging.TextureUtils.ApplyOrientation(_lastFrame, angleCW, mirrorV);

            // Devuelve una copia independiente
            onDone?.Invoke(Texture2D.Instantiate(oriented));
        }

        private IEnumerator AdjustPreviewOrientationNextFrame()
        {
            yield return null; // espera un frame tras Play()
            if (_target == null || _camTex == null) yield break;

            // En UI giramos el rect, y arreglamos el flip con uvRect
            int angle = _camTex.videoRotationAngle; // para RawImage usamos -angle (UI coord)
            _target.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);

            // Si viene espejado vertical, ajustamos el uvRect
            if (_camTex.videoVerticallyMirrored)
                _target.uvRect = new Rect(1, 0, -1, 1); // flip horizontal del UV (compensa espejo vertical de la cam)
            else
                _target.uvRect = new Rect(0, 0, 1, 1);
        }
    }
}
