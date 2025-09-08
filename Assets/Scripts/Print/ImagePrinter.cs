using System;
using System.IO;
using UnityEngine;
using PrintLib; // Plugin Baraonda

namespace dev.xrlab.photoquizz.Printing
{
    public enum FitMode { FitToPage, FitToWidth, FitToHeight, Stretch }

    public class ImagePrinter : MonoBehaviour
    {
        [Header("Printer (opcional)")]
        [Tooltip("Si lo dejas vacío usa la impresora por defecto del sistema.")]
        [SerializeField] private string overridePrinterName = "";

        [Header("Página")]
        [Tooltip("Usar tamaño personalizado (mm). Si es false, se usará el tamaño que reporte el driver.")]
        [SerializeField] private bool useCustom46 = true;     // 4x6" (10x15 cm)
        [SerializeField] private float paperWidthMm = 101.6f;
        [SerializeField] private float paperHeightMm = 152.4f;
        [SerializeField] private int dpi = 300;
        [SerializeField] private int copies = 1;

        [Header("Márgenes (mm)")]
        [SerializeField] private float marginMm = 0f; // deja 0 si quieres intentar borderless

        [Header("Ajuste de imagen")]
        [SerializeField] private FitMode fitMode = FitMode.FitToPage;
        [SerializeField] private bool centerImage = true;

        private Printer printer;

        private void Awake()
        {
            printer = new Printer();
        }

        /// <summary>
        /// Imprime un archivo de imagen (PNG/JPG). Devuelve true si el envío fue OK.
        /// </summary>
        public bool PrintFile(string filePath, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                error = "Archivo no encontrado: " + filePath;
                return false;
            }

            try
            {
                // (0) Selección de impresora (si se especifica)
                if (!string.IsNullOrWhiteSpace(overridePrinterName))
                    printer.SelectPrinter(overridePrinterName);

                // (1) Configurar impresora (tamaño, dpi, color, copias, dúplex).
                //     El manual permite dos overloads: con PaperFormat o con ancho/alto custom (mm). 
                if (useCustom46)
                {
                    printer.SetPrinterSettings(
                        Orientation.Portrait,
                        paperWidthMm,
                        paperHeightMm,
                        dpi,
                        ColorMode.Color,
                        copies,
                        DuplexMode.Default
                    );
                }
                else
                {
                    // Si no usamos custom, solo fijamos orientación/dpi/copias y dejamos que el driver decida el tamaño.
                    printer.SetPrinterSettings(
                        Orientation.Portrait,
                        PaperFormat.A4,     // valor de cortesía; el driver puede tener otro por defecto
                        dpi,
                        ColorMode.Color,
                        copies,
                        DuplexMode.Default
                    );
                }

                // (2) Tamaño REAL aceptado por el driver (mm). Si no aceptó el custom, aquí sabremos lo que realmente se usará. 
                float pageW = 0f, pageH = 0f; // algunas versiones del plugin reportan float
                printer.GetPageSize(ref pageW, ref pageH);

                // Determinar tamaño "en uso": respeta el custom si lo aceptó, si no, cae a lo reportado
                float useW = (useCustom46 ? paperWidthMm : pageW);
                float useH = (useCustom46 ? paperHeightMm : pageH);
                if (Mathf.Abs(pageW - useW) > 1f || Mathf.Abs(pageH - useH) > 1f)
                {
                    useW = pageW;
                    useH = pageH;
                }

                // (3) Área imprimible (restando márgenes)
                float maxW = Mathf.Max(1f, useW - marginMm * 2f);
                float maxH = Mathf.Max(1f, useH - marginMm * 2f);

                // (4) Leer tamaño del PNG para conocer su relación de aspecto
                int imgWpx, imgHpx;
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tmp.LoadImage(bytes, false);
                    imgWpx = tmp.width; imgHpx = tmp.height;
                    Destroy(tmp);
                }
                float imgAR = Mathf.Max(0.0001f, (float)imgWpx / imgHpx);

                // (5) Calcular tamaño objetivo según FitMode (en mm)
                float targetW = maxW, targetH = maxH;

                switch (fitMode)
                {
                    case FitMode.Stretch:
                        targetW = maxW; targetH = maxH; // llena todo, puede deformar
                        break;

                    case FitMode.FitToWidth:
                        targetW = maxW;
                        targetH = targetW / imgAR;
                        if (targetH > maxH) { targetH = maxH; targetW = targetH * imgAR; }
                        break;

                    case FitMode.FitToHeight:
                        targetH = maxH;
                        targetW = targetH * imgAR;
                        if (targetW > maxW) { targetW = maxW; targetH = targetW / imgAR; }
                        break;

                    case FitMode.FitToPage:
                    default:
                        if (maxW / maxH > imgAR)
                        {
                            targetH = maxH;
                            targetW = targetH * imgAR;
                        }
                        else
                        {
                            targetW = maxW;
                            targetH = targetW / imgAR;
                        }
                        break;
                }

                // (6) Posición (origen top-left). Centrar opcional.
                float left = marginMm;
                float top = marginMm;
                if (centerImage)
                {
                    left += (maxW - targetW) * 0.5f;
                    top += (maxH - targetH) * 0.5f;
                }

                // (7) Imprimir: siempre entre StartDocument() y EndDocument() en mm.  
                printer.StartDocument();
                printer.SetPrintPosition(left, top);
                int status = printer.PrintImageFromFile(filePath, targetW, targetH);
                printer.EndDocument();

                if (status < 0)
                {
                    error = printer.GetLastGdiStatus();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}