using System.Collections;
using System.IO;
using UnityEngine;

namespace dev.xrlab.photoquizz.Core
{
    public class UISnapshot : MonoBehaviour
    {
        // Llama: StartCoroutine(CaptureRectToPng(photoRefRect, "C:/tmp/photo_ref.png"));
        public IEnumerator CaptureRectToPng(RectTransform target, string filePath, UnityEngine.Camera uiCamera = null)
        {
            if (target == null) yield break;

            // Esperar al final del frame para que todo esté dibujado
            yield return new WaitForEndOfFrame();

            // 1) Rect de pantalla del RectTransform
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            // Para Screen Space - Overlay pasa null; para Screen Space - Camera pasa la cámara del Canvas
            UnityEngine.Camera cam = uiCamera; // puede ser null (Overlay)
            Vector2 s0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // bottom-left
            Vector2 s2 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // top-right

            int x = Mathf.RoundToInt(Mathf.Min(s0.x, s2.x));
            int y = Mathf.RoundToInt(Mathf.Min(s0.y, s2.y));
            int w = Mathf.RoundToInt(Mathf.Abs(s2.x - s0.x));
            int h = Mathf.RoundToInt(Mathf.Abs(s2.y - s0.y));

            // Clampear a la pantalla por seguridad
            x = Mathf.Clamp(x, 0, Screen.width);
            y = Mathf.Clamp(y, 0, Screen.height);
            w = Mathf.Clamp(w, 1, Screen.width - x);
            h = Mathf.Clamp(h, 1, Screen.height - y);

            // 2) Leer píxeles y guardar PNG
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            tex.Apply(false, false);

            byte[] bytes = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"[UISnapshot] Saved: {filePath} ({w}x{h})");
        }
    }
}
