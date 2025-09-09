using System;
using System.IO;
using UnityEngine;
using PrintLib; // Plugin Baraonda

namespace dev.xrlab.photoquizz.Printing
{
    public enum FitMode { FitToPage, FitToWidth, FitToHeight, Stretch }

    /// <summary>
    /// Imprime imágenes respetando la configuración actual del driver de Windows.
    /// - No llama a SetPrinterSettings: usa tamaño/medio/calidad/ orientación del driver.
    /// - Inicia StartDocument() antes de consultar GetPageSize() para asegurar contexto.
    /// - Fit proporcional + centrado dentro del área imprimible (mm).
    /// - Fallback si el driver devuelve 0x0 mm.
    /// </summary>
    public class ImagePrinter : MonoBehaviour
    {
        [Header("Printer (opcional)")]
        [Tooltip("Si lo dejas vacío usa la impresora por defecto de Windows.")]
        [SerializeField] private string overridePrinterName = "";

        [Header("Ajuste de imagen")]
        [SerializeField] private FitMode fitMode = FitMode.FitToPage;
        [SerializeField] private bool centerImage = true;

        [Header("Márgenes (mm)")]
        [Tooltip("0 si usas borderless en el driver. Aumenta si el modelo tiene margen mínimo.")]
        [SerializeField] private float marginMm = 0f;

        private Printer printer;

        private void Awake()
        {
            printer = new Printer();
        }

        /// <summary>
        /// Imprime un archivo (PNG/JPG) usando la configuración actual del driver de Windows.
        /// Devuelve true si el envío fue OK; si no, 'error' trae el detalle del driver/plugin.
        /// </summary>
        public bool PrintFile(string filePath, out string error)
        {
            error = null;

            // Validación de ruta
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                error = "Archivo no encontrado: " + filePath;
                return false;
            }

            try
            {
                // (0) Seleccionar impresora (opcional). No cambiamos settings del driver.
                if (!string.IsNullOrWhiteSpace(overridePrinterName))
                    printer.SelectPrinter(overridePrinterName); // si no, usa predeterminada. 

                int status = 0;
                float pageW = 0f, pageH = 0f;

                try
                {
                    // (A) Abrir documento primero para asegurar contexto del driver. 
                    printer.StartDocument();

                    // (B) Tamaño real (mm) reportado por el driver. Si no hay contexto, algunos devuelven 0x0. 
                    printer.GetPageSize(ref pageW, ref pageH);

                    // (C) Fallback defensivo si el driver sigue mudo (0x0).
                    if (pageW <= 0f || pageH <= 0f)
                    {
                        // Fallback 4x6 (puedes cambiar por Carta: 215.9 x 279.4)
                        pageW = 101.6f;   // 4"
                        pageH = 152.4f;   // 6"
                        Debug.LogWarning("[Print] GetPageSize devolvió 0x0; usando fallback 4x6 mm.");
                    }

                    // (D) Área imprimible (restando márgenes)
                    float maxW = Mathf.Max(1f, pageW - marginMm * 2f);
                    float maxH = Mathf.Max(1f, pageH - marginMm * 2f);

                    // (E) Tamaño de la imagen para obtener la relación de aspecto
                    int imgWpx, imgHpx;
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tmp.LoadImage(bytes, false);
                        imgWpx = tmp.width; imgHpx = tmp.height;
                        Destroy(tmp);
                    }
                    float imgAR = Mathf.Max(0.0001f, (float)imgWpx / imgHpx);

                    // (F) Calcular tamaño objetivo según FitMode (en mm)
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
                            if (maxW / maxH > imgAR) { targetH = maxH; targetW = targetH * imgAR; }
                            else { targetW = maxW; targetH = targetW / imgAR; }
                            break;
                    }

                    // (G) Posición: origen top-left en mm; centrado opcional
                    float left = marginMm;
                    float top = marginMm;
                    if (centerImage)
                    {
                        left += (maxW - targetW) * 0.5f;
                        top += (maxH - targetH) * 0.5f;
                    }

                    // (H) Imprimir desde disco (en mm), dentro de StartDocument/EndDocument
                    printer.SetPrintPosition(left, top); // mueve el cursor (mm). 
                    status = printer.PrintImageFromFile(filePath, targetW, targetH); // tamaño en mm. 
                }
                finally
                {
                    // Cerrar documento siempre, incluso si hubo excepción. 
                    try { printer.EndDocument(); } catch { /* asegurar cierre */ }
                }

                if (status < 0)
                {
                    // Mensaje del sistema/driver (GDI status) expuesto por el plugin. 
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
