using System.Collections;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Camera
{
    public interface ICameraAdapter
    {
        void StartPreview(RawImage target);
        void StopPreview();
        IEnumerator CapturePhoto(System.Action<UnityEngine.Texture2D> onDone);
    }
}