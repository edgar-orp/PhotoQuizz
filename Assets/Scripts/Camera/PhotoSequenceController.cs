using dev.xrlab.photoquizz.Audio;
using dev.xrlab.photoquizz.Camera;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Core
{
    public class PhotoSequenceController : MonoBehaviour
    {
        public event System.Action<List<Texture2D>> OnSequenceCompleted;

        [Header("Dependencies")]
        [SerializeField] private CameraManager cameraManager;

        [Header("CircleTimer (children)")]
        [SerializeField] private GameObject digit3; // hijo con Image "3"
        [SerializeField] private GameObject digit2; // hijo con Image "2"
        [SerializeField] private GameObject digit1; // hijo con Image "1"

        [Header("Sequence Settings")]
        [SerializeField] private int photosToTake = 4;
        [SerializeField] private int countdownFrom = 3;   // 3 -> 2 -> 1
        [SerializeField] private float delayBetweenPhotos = 0.3f;

        [SerializeField] private Image flashOverlay;      // arrástralo desde el Canvas
        [SerializeField, Range(0f, 1f)] private float flashAlpha = 1f;
        [SerializeField] private float flashDuration = 0.08f; // 80 ms
        [SerializeField] private float flashFadeOut = 0.10f;  // 100 ms de desvanecido

        // Resultado en memoria (temporal)
        public List<Texture2D> Captured;

        private Coroutine running;

        private void Awake()
        {
            if (flashOverlay != null)
            {
                var c = flashOverlay.color;
                c.a = 0f;
                flashOverlay.color = c;
                flashOverlay.enabled = false;
                flashOverlay.raycastTarget = false;
            }
        }

        public void StartPhotoSequence()
        {
            if (running != null) return;
            if (cameraManager == null) return;

            running = StartCoroutine(CaptureRoutine());
        }

        public void StopSequence()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }
            HideAllDigits();
        }

        private IEnumerator CaptureRoutine()
        {
            if (Captured == null) Captured = new List<Texture2D>(photosToTake);
            Captured.Clear();
            HideAllDigits();

            for (int i = 0; i < photosToTake; i++)
            {
                // Countdown 3 -> 2 -> 1 (encender solo el dígito correspondiente)
                for (int c = countdownFrom; c >= 1; c--)
                {
                    ShowDigit(c);
                    yield return new WaitForSeconds(1f);
                }

                // Oculta dígitos justo antes de capturar
                HideAllDigits();

                // Tomar foto
                var photo = cameraManager.CapturePhoto();
                if (photo != null) Captured.Add(photo);

                SoundManager.Instance.Play("CameraShooting");
                yield return StartCoroutine(FlashOnce()); // flash visual

                if (i < photosToTake - 1)
                    yield return new WaitForSeconds(delayBetweenPhotos);
            }

            HideAllDigits();
            running = null;
            OnSequenceCompleted?.Invoke(Captured);
        }

        // === Helpers de UI (encender/apagar hijos del CircleTimer) ===
        private void ShowDigit(int n)
        {
            if (digit3) digit3.SetActive(n == 3);
            if (digit2) digit2.SetActive(n == 2);
            if (digit1) digit1.SetActive(n == 1);
        }

        private void HideAllDigits()
        {
            if (digit3) digit3.SetActive(false);
            if (digit2) digit2.SetActive(false);
            if (digit1) digit1.SetActive(false);
        }

        private IEnumerator FlashOnce()
        {
            if (flashOverlay == null) yield break;

            flashOverlay.enabled = true;

            // subir alpha instantáneo
            var c = flashOverlay.color;
            c.a = flashAlpha;
            flashOverlay.color = c;

            // mantener un instante
            yield return new WaitForSeconds(flashDuration);

            // desvanecer
            float t = 0f;
            while (t < flashFadeOut)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(flashAlpha, 0f, t / flashFadeOut);
                flashOverlay.color = c;
                yield return null;
            }

            // apagar
            c.a = 0f;
            flashOverlay.color = c;
            flashOverlay.enabled = false;
        }
    }
}