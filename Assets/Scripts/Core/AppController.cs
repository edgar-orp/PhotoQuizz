using dev.xrlab.photoquizz.Camera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        // --- Auto Restart ---
        [Header("Auto Restart")]
        [SerializeField] private bool AutoRestartEnabled = true;
        [SerializeField] private float AutoRestartDelaySeconds = 5f;
        [SerializeField] private bool UseHardRestartIfStuck = false; // opcional: fallback a recargar escena

        private Coroutine autoRestartRoutine;

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
        }

        public void OnStartScreen01_Next()
        {
            // Botón de la pantalla 01 que envía a la 02
            ShowStartScreen(2);
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
        }

        private void OnQuizCompleted(ProfileId perfil)
        {
            Debug.Log("[App] Perfil final: " + perfil);
            SetState(AppState.Result);

            if (QuizzPanel) QuizzPanel.SetActive(false);

            if (ProfileManager) ProfileManager.Begin(perfil);
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

            // Guardar PNG del área Photo_Ref
            if (Snapshot != null && PhotoRef != null)
            {
                string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string dir = Path.Combine(pictures, "xrlab.dev");
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = Path.Combine(dir, $"photo_{stamp}.png");

                // Guardamos la referencia del último snapshot para imprimirla
                lastSnapshotPath = path;

                StartCoroutine(Snapshot.CaptureRectToPng(PhotoRef, path, UICamera));
            }

            if (readyDelayRoutine != null) StopCoroutine(readyDelayRoutine);
            readyDelayRoutine = StartCoroutine(GoToReadyAfterDelay());
        }

        private System.Collections.IEnumerator GoToReadyAfterDelay()
        {
            yield return new WaitForSeconds(ReadyDelaySeconds);
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

            // 2) Detener secuencia y limpiar sus dígitos/countdown
            if (PhotoSequence != null)
            {
                PhotoSequence.StopSequence(); // apaga digit3/2/1 y corta coroutine interna
                if (PhotoSequence.Captured != null)
                {
                    // Liberar las texturas capturadas (evita leak entre sesiones)
                    foreach (var tex in PhotoSequence.Captured)
                    {
                        if (tex != null) Destroy(tex);
                    }
                    PhotoSequence.Captured.Clear();
                }
            }

            // 3) Limpiar las sprites del Review para no retener texturas
            ClearImage(ReviewImage1);
            ClearImage(ReviewImage2);
            ClearImage(ReviewImage3);
            ClearImage(ReviewImage4);

            // 4) Detener preview de cámara (si quieres volver directo a Preview, puedes relanzarla luego)
            if (CameraManager != null)
                CameraManager.StopPreview();

            // 5) Resetear controles visibles en Section02 por si estaban activos
            if (TakePhotoButton) TakePhotoButton.SetActive(true);
            if (CircleTimer) CircleTimer.SetActive(false);

            // 6) Olvidar el último snapshot si lo guardabas
            lastSnapshotPath = null;

            // 7) Volver a Intro (Section01) y estado Home
            ShowPhotoSection(1);
            SetState(AppState.Home);

            // 8) Opcional: liberar memoria no referenciada
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
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
    }
}