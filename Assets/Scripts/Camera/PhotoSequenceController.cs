using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using dev.xrlab.photoquizz.Camera;

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

        // Resultado en memoria (temporal)
        public List<Texture2D> Captured;

        private Coroutine running;

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
    }
}