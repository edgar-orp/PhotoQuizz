using System;
using System.IO;
using UnityEngine;
using PrintLib; // Plugin Baraonda

namespace dev.xrlab.photoquizz.Printing
{
    public class ImagePrinter : MonoBehaviour
    {
        [Header("Printer (opcional)")]
        [SerializeField] private string overridePrinterName = "";

        [Header("Página")]
        [SerializeField] private bool useCustom46 = true;      // 4x6" (10x15 cm)
        [SerializeField] private float paperWidthMm = 101.6f; // 4.0"
        [SerializeField] private float paperHeightMm = 152.4f; // 6.0"
        [SerializeField] private int dpi = 300;
        [SerializeField] private int copies = 1;

        [Header("Margen (mm)")]
        [SerializeField] private float marginMm = 4f;

        private Printer printer;

        private void Awake()
        {
            printer = new Printer();
        }

        /// <summary>Imprime un PNG desde disco. true si el envío fue OK.</summary>
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
                if (!string.IsNullOrWhiteSpace(overridePrinterName))
                    printer.SelectPrinter(overridePrinterName);

                // Configurar página / copias
                if (useCustom46)
                {
                    // Versión con tamaño personalizado (en mm)
                    printer.SetPrinterSettings(
                        Orientation.Portrait,
                        paperWidthMm,
                        paperHeightMm,
                        dpi,
                        ColorMode.Color,
                        copies,
                        DuplexMode.Default  // <- aquí el cambio
                    );
                }
                else
                {
                    // Ejemplo: Letter
                    printer.SetPrinterSettings(
                        Orientation.Portrait,
                        PaperFormat.Letter,
                        dpi,
                        ColorMode.Color,
                        copies,
                        DuplexMode.Default  // <- aquí el cambio
                    );
                }

                // Tamaño de página reportado por la impresora (en mm) — en tu versión son floats
                float pageW = 0f, pageH = 0f; // <- aquí el cambio
                printer.GetPageSize(ref pageW, ref pageH); // <- aquí el cambio

                // Documento: abrir → posicionar → imprimir → cerrar
                printer.StartDocument();

                // Área disponible = página - márgenes
                float useW = useCustom46 ? paperWidthMm : pageW;
                float useH = useCustom46 ? paperHeightMm : pageH;

                float availW = Mathf.Max(1f, useW - marginMm * 2f);
                // Posición al margen superior-izquierdo
                printer.SetPrintPosition(marginMm, marginMm);

                // Escalamos por ancho y mantenemos proporción (alto=0)
                int status = printer.PrintImageFromFile(filePath, availW, 0);

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
