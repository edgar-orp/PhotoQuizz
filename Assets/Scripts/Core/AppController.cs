using dev.xrlab.photoquizz.Camera;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Core
{
    public class AppController : MonoBehaviour
    {
        [SerializeField] private AppState state = AppState.Boot;
        public AppState State => state;

        [Header("Camera")]
        [SerializeField] private CameraManager CameraManager;

        [Header("Photo Sequence")]
        [SerializeField] private PhotoSequenceController PhotoSequence;

        // --- UI: Photo ---
        [Header("Photo Panel")]
        [SerializeField] private GameObject PhotoPanel;

        [Header("Photo Sections (01..04)")]
        [SerializeField] private GameObject Section01; // Intro
        [SerializeField] private GameObject Section02; // Preview + Countdown
        [SerializeField] private GameObject Section03; // Review
        [SerializeField] private GameObject Section04; // Ready

        [Header("UI Elements (Section02)")]
        [SerializeField] private GameObject TakePhotoButton;
        [SerializeField] private GameObject CircleTimer;

        [Header("Review (Section03)")]
        [SerializeField] private Image ReviewImage1;
        [SerializeField] private Image ReviewImage2;
        [SerializeField] private Image ReviewImage3;
        [SerializeField] private Image ReviewImage4;

        private void Awake()
        {
            SetState(AppState.Boot);

            if (PhotoSequence != null)
                PhotoSequence.OnSequenceCompleted += OnPhotosCaptured;
        }

        private void Start()
        {
            OnEnterPhotoIntro();
        }

        // ===== Transiciones enfocadas en Photo =====

        public void OnEnterPhotoIntro()
        {
            SetState(AppState.Home);

            if (PhotoPanel) PhotoPanel.SetActive(true);
            ShowPhotoSection(1);
        }

        public void OnStartCamera()
        {
            SetState(AppState.Preview);

            ShowPhotoSection(2);
            if (CameraManager) CameraManager.StartPreview();

            if (TakePhotoButton) TakePhotoButton.SetActive(true);
            if (CircleTimer) CircleTimer.SetActive(false);
        }

        public void OnStartPhotoSequence()
        {
            SetState(AppState.Capturing);

            // Seguimos en Section02 y alternamos elementos
            ShowPhotoSection(2);
            if (TakePhotoButton) TakePhotoButton.SetActive(false);
            if (CircleTimer) CircleTimer.SetActive(true);

            if (PhotoSequence) PhotoSequence.StartPhotoSequence();
        }
        private void OnPhotosCaptured(List<Texture2D> photos)
        {
            Debug.Log($"[AppController] Secuencia terminada con {photos.Count} fotos");

            AssignToImage(ReviewImage1, photos, 0);
            AssignToImage(ReviewImage2, photos, 1);
            AssignToImage(ReviewImage3, photos, 2);
            AssignToImage(ReviewImage4, photos, 3);

            OnReviewPhotos();
        }

        public void OnReviewPhotos()
        {
            SetState(AppState.Review);
            ShowPhotoSection(3);
        }

        // ===== Helpers =====

        private void ShowPhotoSection(int index)
        {
            if (Section01) Section01.SetActive(index == 1);
            if (Section02) Section02.SetActive(index == 2);
            if (Section03) Section03.SetActive(index == 3);
            if (Section04) Section04.SetActive(index == 4);
        }

        private void SetState(AppState newState)
        {
            if (state == newState) return;
            state = newState;
            Debug.Log($"[AppController] State -> {state}");
            OnStateChanged(state);
        }

        private void OnStateChanged(AppState newState)
        {
            // Hook para sincronizar efectos globales si los necesitas después
        }

        private static void AssignToImage(Image target, System.Collections.Generic.List<Texture2D> list, int index)
        {
            if (!target) return;
            if (list == null || index >= list.Count || list[index] == null)
            {
                target.sprite = null;
                return;
            }

            var tex = list[index];
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            target.sprite = sp;
            target.preserveAspect = true;
        }
    }
}
