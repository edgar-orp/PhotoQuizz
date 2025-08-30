using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using dev.xrlab.photoquizz.Camera;
using dev.xrlab.photoquizz.Imaging;
using dev.xrlab.photoquizz.Print;
using PrintLib;

namespace dev.xrlab.photoquizz.Core
{
    public class AppController : MonoBehaviour
    {
        [Header("References")]
        public CameraManager cameraManager;
        public RawImage previewTarget;
        public Texture2D frameOverlay; // opcional, 1800x1200 con alpha

        private MosaicBuilder _mosaicBuilder;
        private Composer _composer;
        private PrinterService _printer;

        private void Awake()
        {
            _mosaicBuilder = new MosaicBuilder();
            _composer = new Composer();
            _printer = new PrinterService(new BaraondaPrinterAdapter
            {
                orientation = Orientation.Landscape, // o Portrait
                pageWidthMM = 152f,
                pageHeightMM = 102f,
                dpi = 300,
                colorMode = ColorMode.Color,
                copies = 1,
                preferredPrinterName = null // o el nombre exacto si quieres fijarla
            });
        }

        private void Start()
        {
            if (cameraManager != null)
                cameraManager.StartPreview();
        }

        // Llamar desde UI (botón “Tomar fotos e imprimir”)
        public void OnStartPhotoSequence()
        {
            StartCoroutine(CaptureAndPrintRoutine());
        }

        private IEnumerator CaptureAndPrintRoutine()
        {
            List<Texture2D> photos = null;
            yield return StartCoroutine(cameraManager.TakePhotos(4, list => photos = list));

            var mosaic = _mosaicBuilder.Build(photos);
            var finalImg = _composer.Compose(mosaic, frameOverlay);

            if (previewTarget) previewTarget.texture = finalImg;

            bool ok = _printer.TryPrint(finalImg, out string savedPath);
            if (!ok)
            {
                Debug.Log("[UI] TODO: Mostrar botón 'Retry' y reutilizar 'final' para reintentar.");
                // Ejemplo posterior de retry:
                // ok = _printer.TryPrint(finalImg, out savedPath);
            }
            else
            {
                Debug.Log("[Print] Impresión enviada correctamente.");
            }
        }
    }
}
