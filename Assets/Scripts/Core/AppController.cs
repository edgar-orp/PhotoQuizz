using dev.xrlab.photoquizz.Camera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using dev.xrlab.photoquizz.Audio;

namespace dev.xrlab.photoquizz.Core
{
    public class AppController : MonoBehaviour
    {
        [SerializeField] private AppState state = AppState.Boot;
        public AppState State => state;

        [Header("Quizz Manager")]
        [SerializeField] private QuizManager QuizManager;

        [Header("Camera")]
        [SerializeField] private CameraManager CameraManager;

        [Header("Photo Sequence")]
        [SerializeField] private PhotoSequenceController PhotoSequence;

        // --- Start ---
        [Header("Start / Quiz")]
        [SerializeField] private GameObject StartPanel;      // Panel raíz de arranque
        [SerializeField] private GameObject StartScreen01;   // Primera pantalla (01)
        [SerializeField] private GameObject StartScreen02;   // Segunda pantalla (02)

        // --- Quiz Panel ---

        [SerializeField] private GameObject QuizzPanel;      // Panel donde inician las preguntas

        // --- Results Profiles ---

        [Header("Results/Profile")]
        [SerializeField] private ProfileManager ProfileManager;

        // --- UI: Photo ---
        [Header("Photo Panel")]
        [SerializeField] private GameObject PhotoPanel;

        [Header("Photo Sections (01..04)")]
        [SerializeField] private GameObject Section01; // Intro
        [SerializeField] private GameObject Section02; // Preview + Countdown
        [SerializeField] private GameObject Section03; // Review
        [SerializeField] private GameObject Section04; // Ready / Print

        [Header("UI Elements (Section02)")]
        [SerializeField] private GameObject TakePhotoButton;
        [SerializeField] private GameObject CircleTimer;

        [Header("Review (Section03)")]
        [SerializeField] private Image ReviewImage1;
        [SerializeField] private Image ReviewImage2;
        [SerializeField] private Image ReviewImage3;
        [SerializeField] private Image ReviewImage4;

        // --- Photo: Profile badge ---
        [Header("Photo: Profile Badge")]
        [SerializeField] private Image Profile_Logo; // Image dentro del área capturable (Photo_Ref)
        [SerializeField] private Sprite Sprite_Emocional;
        [SerializeField] private Sprite Sprite_Social;
        [SerializeField] private Sprite Sprite_Emprendedor;
        [SerializeField] private Sprite Sprite_Influenciable;

        private ProfileId lastProfile = ProfileId.Undefined;

        // --- Snapshot ---
        [Header("Snapshot")]
        [SerializeField] private UISnapshot Snapshot;                 // script con CaptureRectToPng(...)
        [SerializeField] private RectTransform PhotoRef;              // contenedor a capturar (Photo_Ref)
        [SerializeField] private UnityEngine.Camera UICamera = null;  // si tu Canvas es Overlay, deja null

        [Header("Ready/Print")]
        [SerializeField] private float ReadyDelaySeconds = 3f;

        // --- Printing ---
        [Header("Printer")]
        [SerializeField] private dev.xrlab.photoquizz.Printing.ImagePrinter ImagePrinter;

        private Coroutine readyDelayRoutine;
        private string lastSnapshotPath = null;

        // --- Coroutines manejadas ---
        private Coroutine snapshotRoutine;
        private int generation = 0;

        // --- Auto Restart ---
        [Header("Auto Restart")]
        [SerializeField] private bool AutoRestartEnabled = true;
        [SerializeField] private float AutoRestartDelaySeconds = 5f;
        [SerializeField] private bool UseHardRestartIfStuck = false; // opcional: fallback a recargar escena

        private Coroutine autoRestartRoutine;

        [Header("Testing")]
        [SerializeField] private bool SkipPrintForTesting = false;

        private void Awake()
        {
            SetState(AppState.Boot);

            if (PhotoSequence) PhotoSequence.OnSequenceCompleted += OnPhotosCaptured;
            if (QuizManager) QuizManager.QuizCompleted += OnQuizCompleted;
        }

        private void OnDestroy()
        {
            if (PhotoSequence) PhotoSequence.OnSequenceCompleted -= OnPhotosCaptured;
            if (QuizManager) QuizManager.QuizCompleted -= OnQuizCompleted;
        }

        private void Start()
        {
            ActivateStart();
        }

        // ====== Start / Quiz Flow ======
        public void ActivateStart()
        {
            // Estado de inicio
            SetState(AppState.Home);

            // Activar StartPanel y mostrar 01; ocultar lo demás
            if (StartPanel) StartPanel.SetActive(true);
            ShowStartScreen(1);

            if (QuizzPanel) QuizzPanel.SetActive(false);

            // Si quieres ocultar el panel de fotos al arranque:
            if (PhotoPanel) PhotoPanel.SetActive(false);

            SoundManager.Instance.Play("LoadApp");
        }

        public void OnStartScreen01_Next()
        {
            // Botón de la pantalla 01 que envía a la 02
            ShowStartScreen(2);

            SoundManager.Instance.Play("ButtonNext");
        }

        private void ShowStartScreen(int index)
        {
            if (StartScreen01) StartScreen01.SetActive(index == 1);
            if (StartScreen02) StartScreen02.SetActive(index == 2);
        }

        public void OnStartScreen02_BeginQuiz()
        {
            if (StartPanel) StartPanel.SetActive(false);
            if (QuizzPanel) QuizzPanel.SetActive(true);

            SetState(AppState.Quiz);

            // Aquí decides cuál mostrar
            if (QuizManager) QuizManager.ShowQuestion(1);

            SoundManager.Instance.Play("ButtonNext");
        }

        private void OnQuizCompleted(ProfileId perfil)
        {
            Debug.Log("[App] Perfil final: " + perfil);
            SetState(AppState.Result);

            lastProfile = perfil;

            if (QuizzPanel) QuizzPanel.SetActive(false);

            if (ProfileManager) ProfileManager.Begin(perfil);
        }

        // ===== Transiciones enfocadas en Photo =====

        public void OnEnterPhotoIntro()
        {
            SetState(AppState.Home);

            if (PhotoPanel) PhotoPanel.SetActive(true);
            ShowPhotoSection(1);

            SoundManager.Instance.Play("ButtonNext");
        }

        public void OnStartCamera()
        {
            SetState(AppState.Preview);

            ShowPhotoSection(2);
            if (CameraManager) CameraManager.StartPreview();

            if (TakePhotoButton) TakePhotoButton.SetActive(true);
            if (CircleTimer) CircleTimer.SetActive(false);

            SoundManager.Instance.Play("ButtonNext");
        }

        public void OnStartPhotoSequence()
        {
            SetState(AppState.Capturing);

            // Seguimos en Section02 y alternamos elementos
            ShowPhotoSection(2);
            if (TakePhotoButton) TakePhotoButton.SetActive(false);
            if (CircleTimer) CircleTimer.SetActive(true);

            if (PhotoSequence) PhotoSequence.StartPhotoSequence();

            SoundManager.Instance.Play("ButtonNext");
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

            UpdateProfileLogo();

            SoundManager.Instance.Play("CameraComplete");

            // Guardar PNG del área Photo_Ref
            if (Snapshot != null && PhotoRef != null)
            {
                string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string dir = Path.Combine(pictures, "xrlab.dev");
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = Path.Combine(dir, $"photo_{stamp}.png");

                // Guardamos la referencia del último snapshot para imprimirla
                lastSnapshotPath = path;

                if (snapshotRoutine != null) { StopCoroutine(snapshotRoutine); snapshotRoutine = null; }
                snapshotRoutine = StartCoroutine(Snapshot.CaptureRectToPng(PhotoRef, path, UICamera));
            }

            if (readyDelayRoutine != null) StopCoroutine(readyDelayRoutine);
            readyDelayRoutine = StartCoroutine(GoToReadyAfterDelay(generation));
        }

        private System.Collections.IEnumerator GoToReadyAfterDelay(int genAtStart)
        {
            yield return new WaitForSeconds(ReadyDelaySeconds);
            if (genAtStart != generation) yield break; // hubo reset; no continúes
            OnEnterReadyAndPrint();
        }

        private void OnEnterReadyAndPrint()
        {
            // Activar sección 04 y mandar a imprimir
            ShowPhotoSection(4);
            SetState(AppState.Printing);

            StartPrint();
        }

        // ===== Print =====
        private void StartPrint()
        {
            // --- MODO TEST: omitir impresión real y continuar el flujo ---
            if (SkipPrintForTesting)
            {
                Debug.Log("[Print] TEST MODE: omitiendo impresión y continuando como si hubiera impreso.");
                OnPrintCompleted();
                return;
            }

            string pngToPrint = lastSnapshotPath;

            if (string.IsNullOrEmpty(pngToPrint) || !File.Exists(pngToPrint))
            {
                string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string dir = Path.Combine(pictures, "xrlab.dev");
                if (Directory.Exists(dir))
                {
                    var latest = new DirectoryInfo(dir)
                        .GetFiles("photo_*.png")
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault();
                    pngToPrint = latest != null ? latest.FullName : null;
                }
            }

            if (string.IsNullOrEmpty(pngToPrint) || !File.Exists(pngToPrint))
            {
                Debug.LogError("[Print] No se encontró PNG para imprimir.");
                SetState(AppState.Error);
                return;
            }

            if (ImagePrinter == null)
            {
                Debug.LogError("[Print] ImagePrinter no está asignado en el Inspector.");
                SetState(AppState.Error);
                return;
            }

            if (ImagePrinter.PrintFile(pngToPrint, out var err))
            {
                OnPrintCompleted();
            }
            else
            {
                Debug.LogError("[Print] Falló la impresión: " + err);
                SetState(AppState.Error);
                //Arreglar aqui
            }
        }

        private void OnPrintCompleted()
        {
            Debug.Log("[Print] Impresión completada.");
            SetState(AppState.Done);

            if (AutoRestartEnabled)
            {
                if (autoRestartRoutine != null) StopCoroutine(autoRestartRoutine);
                autoRestartRoutine = StartCoroutine(AutoRestartAfterDelay());
            }
        }

        private System.Collections.IEnumerator AutoRestartAfterDelay()
        {
            yield return new WaitForSeconds(AutoRestartDelaySeconds);

            // Si por alguna razón la impresión siguiera “viva”, evita reiniciar en Printing.
            if (State == AppState.Printing) yield break;

            if (UseHardRestartIfStuck)
                HardRestartScene();
            else
                RestartLoop();
        }

        private void CancelAutoRestartIfAny()
        {
            if (autoRestartRoutine != null)
            {
                StopCoroutine(autoRestartRoutine);
                autoRestartRoutine = null;
            }
        }

        public void RestartLoop()
        {
            // 1) No dejar timers colgando
            if (readyDelayRoutine != null)
            {
                StopCoroutine(readyDelayRoutine);
                readyDelayRoutine = null;
            }
            if (autoRestartRoutine != null)
            {
                StopCoroutine(autoRestartRoutine);
                autoRestartRoutine = null;
            }

            // NUEVO: cancelar snapshot en curso
            if (snapshotRoutine != null)
            {
                StopCoroutine(snapshotRoutine);
                snapshotRoutine = null;
            }

            // NUEVO: invalida coroutines anteriores
            generation++;

            // 2) Detener secuencia y limpiar sus dígitos/countdown
            if (PhotoSequence != null)
            {
                PhotoSequence.StopSequence();
                if (PhotoSequence.Captured != null)
                {
                    foreach (var tex in PhotoSequence.Captured)
                        if (tex != null) Destroy(tex);
                    PhotoSequence.Captured.Clear();
                }
            }

            // 3) Limpiar las sprites del Review
            ClearImage(ReviewImage1);
            ClearImage(ReviewImage2);
            ClearImage(ReviewImage3);
            ClearImage(ReviewImage4);

            // 4) Detener preview de cámara
            if (CameraManager != null)
                CameraManager.StopPreview();

            // 5) Resetear controles visibles en Section02
            if (TakePhotoButton) TakePhotoButton.SetActive(true);
            if (CircleTimer) CircleTimer.SetActive(false);

            // 6) Olvidar el último snapshot y el logo de perfil
            lastSnapshotPath = null;
            if (Profile_Logo)
            {
                Profile_Logo.sprite = null;
                Profile_Logo.enabled = false;
            }

            // 7) Ocultar paneles que no son de inicio
            if (PhotoPanel) PhotoPanel.SetActive(false);
            if (QuizzPanel) QuizzPanel.SetActive(false);
            if (ProfileManager) ProfileManager.ResetResults();

            // 8) Opcional: liberar memoria no referenciada
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            // 9) Volver al flujo principal de inicio
            ActivateStart();  // <-- aquí regresas a StartPanel + pantalla 01
        }


        public void HardRestartScene()
        {
            // Idealmente asegúrate de no estar en Printing (espera a que EndDocument haya corrido)
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
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

        private void OnStateChanged(AppState newState) { }

        private static void AssignToImage(Image target, List<Texture2D> list, int index)
        {
            if (!target) return;
            if (list == null || index >= list.Count || list[index] == null)
            {
                target.sprite = null;
                return;
            }

            var tex = list[index];
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), 100f);
            target.sprite = sp;
            target.preserveAspect = true;
        }

        private static void ClearImage(Image img)
        {
            if (!img) return;
            var sp = img.sprite;
            img.sprite = null;
            if (sp != null)
            {
                var tex = sp.texture;
                Destroy(sp);
                // CUIDADO: solo destruye la texture si sabes que fue creada en runtime (tus fotos lo son)
                if (tex != null) Destroy(tex);
            }
        }

        private void UpdateProfileLogo()
        {
            if (!Profile_Logo) return;

            Sprite s = null;
            switch (lastProfile)
            {
                case ProfileId.Emocional: s = Sprite_Emocional; break;
                case ProfileId.Social: s = Sprite_Social; break;
                case ProfileId.Emprendedor: s = Sprite_Emprendedor; break;
                case ProfileId.Influenciable: s = Sprite_Influenciable; break;
                default: s = null; break;
            }

            Profile_Logo.sprite = s;
            // Mostrar sólo si hay sprite asignado
            Profile_Logo.enabled = (s != null);
        }
    }
}