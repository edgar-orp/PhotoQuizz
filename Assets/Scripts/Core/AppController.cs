using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using dev.xrlab.photoquizz.Camera;
using dev.xrlab.photoquizz.Imaging;
using dev.xrlab.photoquizz.Print;
using PrintLib; // Orientation, ColorMode, DuplexMode

namespace dev.xrlab.photoquizz.Core
{
    public class AppController : MonoBehaviour
    {
        [Header("References")]
        public CameraManager cameraManager;
        public RawImage previewTarget;
        [Tooltip("PNG con alpha. Si el layout no coincide, el sistema puede rotarlo/redimensionarlo.")]
        public Texture2D frameOverlay;

        [Header("Layout")]
        public PhotoLayoutOrientation layout = PhotoLayoutOrientation.Landscape; // <-- enum

        [Header("Print Settings")]
        [Range(150, 600)] public int dpi = 300;
        public ColorMode colorMode = ColorMode.Color;
        [Min(1)] public int copies = 1;
        public DuplexMode duplex = DuplexMode.Default; // o (DuplexMode)0
        [Tooltip("Si se deja vacío usa la impresora predeterminada del sistema.")]
        public string preferredPrinterName = null;

        private MosaicBuilder _mosaicBuilder;
        private Composer _composer;
        private PrinterService _printer;

        private bool IsPortrait => layout == PhotoLayoutOrientation.Portrait;

        private void Awake()
        {
            // 1) Builder según layout
            _mosaicBuilder = new MosaicBuilder(layout);

            // 2) Composer
            _composer = new Composer();

            // 3) Adapter de impresión acorde al layout
            float pageWmm, pageHmm;
            GetPageSizeMM(layout, out pageWmm, out pageHmm);

            var adapter = new BaraondaPrinterAdapter
            {
                orientation = IsPortrait ? Orientation.Portrait : Orientation.Landscape,
                pageWidthMM = pageWmm,
                pageHeightMM = pageHmm,
                dpi = dpi,
                colorMode = colorMode,
                copies = copies,
                duplex = duplex,
                preferredPrinterName = preferredPrinterName
            };
            _printer = new PrinterService(adapter);

            // 4) (Opcional) Intentar alinear overlay al layout si es legible
            if (frameOverlay != null)
            {
                try
                {
                    // Si el overlay viene en orientación contraria, lo rotamos 90°
                    bool overlayIsPortrait = frameOverlay.height > frameOverlay.width;
                    if (overlayIsPortrait != IsPortrait)
                        frameOverlay = TextureUtils.Rotate90CW(frameOverlay);
                }
                catch
                {
                    // Si no es "readable", Composer igualmente puede redimensionar/rotar cuando convenga
                    // (gracias a auto-rotación + fit internos en Composer).
                }
            }
        }

        private void Start()
        {
            // Ajustar aspecto visual del preview al papel: 3:2 (Landscape) o 2:3 (Portrait)
            var arf = previewTarget ? previewTarget.GetComponent<AspectRatioFitter>() : null;
            if (arf != null)
            {
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = IsPortrait ? (2f / 3f) : (3f / 2f);
            }

            if (cameraManager != null)
                cameraManager.StartPreview();
        }

        // Botón “Tomar fotos e imprimir”
        public void OnStartPhotoSequence()
        {
            StartCoroutine(CaptureAndPrintRoutine());
        }

        private IEnumerator CaptureAndPrintRoutine()
        {
            List<Texture2D> photos = null;
            yield return StartCoroutine(cameraManager.TakePhotos(4, list => photos = list));

            var mosaic = _mosaicBuilder.Build(photos);
            var finalImg = _composer.Compose(
                mosaic,
                frameOverlay,
                fitOverlay: true,
                autoRotateOverlay: true // por si el asset no se pudo rotar en Awake
            );

            if (previewTarget) previewTarget.texture = finalImg;

            // Siempre guarda PNG; si impresión falla, solo Debug.Log (luego pondrás “Retry”)
            bool ok = _printer.TryPrint(finalImg, out string savedPath);

            if (!ok)
            {
                Debug.Log("[UI] TODO: Mostrar botón 'Retry' para reintentar la impresión.");
            }
            else
            {
                Debug.Log("[Print] Impresión enviada correctamente.");
            }
        }

        /// <summary>
        /// Devuelve tamaño de página en mm según el layout seleccionado.
        /// Landscape = 6x4 (152x102mm), Portrait = 4x6 (102x152mm)
        /// </summary>
        private static void GetPageSizeMM(PhotoLayoutOrientation layout, out float w, out float h)
        {
            if (layout == PhotoLayoutOrientation.Portrait)
            {
                w = 102f; // 4"
                h = 152f; // 6"
            }
            else
            {
                w = 152f; // 6"
                h = 102f; // 4"
            }
        }

        // (Opcional) permitir cambiar layout en runtime y reconfigurar
        public void SetLayout(PhotoLayoutOrientation newLayout)
        {
            if (layout == newLayout) return;
            layout = newLayout;

            // Reconfigurar builder y printer
            _mosaicBuilder = new MosaicBuilder(layout);

            float w, h;
            GetPageSizeMM(layout, out w, out h);
            var adapter = new BaraondaPrinterAdapter
            {
                orientation = IsPortrait ? Orientation.Portrait : Orientation.Landscape,
                pageWidthMM = w,
                pageHeightMM = h,
                dpi = dpi,
                colorMode = colorMode,
                copies = copies,
                duplex = duplex,
                preferredPrinterName = preferredPrinterName
            };
            _printer = new PrinterService(adapter);

            // Ajustar preview AR
            var arf = previewTarget ? previewTarget.GetComponent<AspectRatioFitter>() : null;
            if (arf != null) arf.aspectRatio = IsPortrait ? (2f / 3f) : (3f / 2f);
        }
    }
}
