using dev.xrlab.photoquizz.Camera;
using UnityEngine;
using UnityEngine.UI;

namespace dev.xrlab.photoquizz.Core
{
    public class AppController : MonoBehaviour
    {
        [SerializeField] private AppState state = AppState.Boot;
        public AppState State => state;

        // --- UI: Photo ---
        [Header("Photo Panel")]
        [SerializeField] private GameObject PhotoPanel;

        [Header("Photo Sections (01..04)")]
        [SerializeField] private GameObject Section01;
        [SerializeField] private GameObject Section02;
        [SerializeField] private GameObject Section03;
        [SerializeField] private GameObject Section04;

        [Header("Camera")]
        [SerializeField] private CameraManager CameraManager;

        private void Awake()
        {
            SetState(AppState.Boot);
        }

        private void Start()
        {
            OnEnterPhoto();
        }

        // ===== Transiciones enfocadas en Photo =====

        public void OnEnterPhoto()
        {
            SetState(AppState.Photo);

            // Activar panel principal
            if (PhotoPanel) PhotoPanel.SetActive(true);

            // Mostrar solo la sección 01
            ShowPhotoSection(1);
        }

        public void OnStartCamera()
        {
            ShowPhotoSection(2);
            if (CameraManager) CameraManager.StartPreview();
        }

        public void OnStartPhotoSequence()
        {
            ShowPhotoSection(3);
        }

        public void CancelToPhoto()
        {
            SetState(AppState.Photo);
            ShowPhotoSection(1);
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
            // Por ahora no hacemos más; más adelante puedes notificar a un ScreenManager
        }
    }
}