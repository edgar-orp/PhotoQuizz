using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace dev.xrlab.photoquizz.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [Header("UI")]
        public RawImage previewTarget;
        public TMP_Text countdownText;  // TextMeshPro

        [Header("Settings")]
        public float betweenShotsSeconds = 0.4f;

        private ICameraAdapter _adapter;

        private void Awake()
        {
            _adapter = GetComponent<ICameraAdapter>();
            if (_adapter == null)
                _adapter = gameObject.AddComponent<UnityCameraAdapter>();
        }

        public void StartPreview() => _adapter.StartPreview(previewTarget);
        public void StopPreview() => _adapter.StopPreview();

        public IEnumerator TakePhotos(int count, System.Action<List<Texture2D>> onAllDone)
        {
            var photos = new List<Texture2D>(count);

            for (int i = 0; i < count; i++)
            {
                yield return StartCoroutine(DoCountdown(3));
                yield return StartCoroutine(_adapter.CapturePhoto(tex => { if (tex) photos.Add(tex); }));
                yield return new WaitForSeconds(betweenShotsSeconds);
            }

            onAllDone?.Invoke(photos);
        }

        private IEnumerator DoCountdown(int seconds)
        {
            for (int s = seconds; s >= 1; s--)
            {
                if (countdownText) countdownText.text = s.ToString();
                yield return new WaitForSeconds(1f);
            }
            if (countdownText) countdownText.text = string.Empty;
        }
    }
}
