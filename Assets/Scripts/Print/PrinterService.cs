using UnityEngine;

namespace dev.xrlab.photoquizz.Print
{
    public class PrinterService
    {
        private readonly IPrinterAdapter _adapter;

        public PrinterService(IPrinterAdapter adapter)
        {
            _adapter = adapter;
        }

        /// <summary>
        /// 1) Siempre guarda el PNG en Imágenes\{subfolder} (con fallback a persistente).
        /// 2) Intenta imprimir.
        /// 3) Si falla, solo hace Debug.Log (ahí se colocará el "Retry" de UI).
        /// Devuelve true si la impresión fue enviada OK.
        /// </summary>
        public bool TryPrint(Texture2D finalImage, out string savedPath)
        {
            // Guardado SIEMPRE (silencioso, con fallback)
            savedPath = FileSaver.SavePngPreferPicturesSilent(
                finalImage,
                fileNamePrefix: "print_",
                subfolderName: "XRlab PhotoQuizz"
            );

            if (string.IsNullOrEmpty(savedPath))
            {
                Debug.LogError("[Print] No se pudo guardar la imagen (Pictures ni fallback).");
            }
            else
            {
                Debug.Log($"[Print] Imagen guardada en: {savedPath}");
            }

            // Intentar impresión
            bool ok = _adapter?.Print(finalImage) == true;

            if (!ok)
            {
                // NO abrimos diálogos ni hacemos más acciones: solo log.
                Debug.Log("[Print] Falló la impresión. (Aquí mostraremos el botón 'Retry')");
            }

            return ok;
        }

        /// <summary>
        /// Guardar sin imprimir (útil para QA o modo sin impresora).
        /// </summary>
        public string SaveOnly(Texture2D finalImage, string prefix = "print_", string subfolder = "XRlab PhotoQuizz")
        {
            return FileSaver.SavePngPreferPicturesSilent(finalImage, prefix, subfolder);
        }
    }
}
