using UnityEngine;
using PrintLib;

namespace dev.xrlab.photoquizz.Print
{
    /// Imprime una Texture2D a 4x6" (152x102 mm) sin diálogos.
    public class BaraondaPrinterAdapter : IPrinterAdapter
    {
        // Cambia a Portrait + 102x152 si quieres 6x4 vertical
        public Orientation orientation = Orientation.Landscape;
        public float pageWidthMM = 152f;   // 6"
        public float pageHeightMM = 102f;   // 4"
        public int dpi = 300;
        public ColorMode colorMode = ColorMode.Color;
        public int copies = 1;
        public DuplexMode duplex = DuplexMode.Default; // o (DuplexMode)0
        public string preferredPrinterName = null;      // null => predeterminada

        public bool Print(Texture2D img)
        {
            if (img == null) return false;

            try
            {
                Texture2D printable = EnsureReadableRGBA32(img);

                var printer = new Printer();

                // (Opcional) seleccionar impresora por nombre exacto
                if (!string.IsNullOrEmpty(preferredPrinterName))
                    printer.SelectPrinter(preferredPrinterName);

                // --- Ajustar tamaños según orientación (algunos drivers lo requieren) ---
                float w = pageWidthMM;
                float h = pageHeightMM;
                if (orientation == Orientation.Landscape && w < h) { var t = w; w = h; h = t; }
                if (orientation == Orientation.Portrait && w > h) { var t = w; w = h; h = t; }

                // --- Intentar setear ajustes; si falla, continuamos igual ---
                bool settingsOk = true;
                try
                {
                    printer.SetPrinterSettings(orientation, w, h, dpi, colorMode, copies, duplex);
                }
                catch (System.Exception e)
                {
                    settingsOk = false;
                    Debug.LogWarning($"[Baraonda] SetPrinterSettings falló: {e.Message}. Se continúa con ajustes por defecto del driver.");
                }

                // --- Documento e impresión ---
                printer.StartDocument();
                printer.SetPrintPosition(0f, 0f); // origen en mm (arriba-izquierda)
                int rc = printer.PrintTexture(printable, w, h);
                printer.EndDocument();

                // --- Evaluar estado: rc==0 y GDI == "Ok" (o vacío) => éxito ---
                string gdi = printer.GetLastGdiStatus();
                bool gdiOk = string.IsNullOrEmpty(gdi) || gdi.Equals("Ok", System.StringComparison.OrdinalIgnoreCase);
                bool ok = (rc == 0) && gdiOk;

                if (!ok)
                {
                    Debug.LogError($"[Baraonda] Error al imprimir. rc={rc}, GDI='{gdi}', settingsOk={settingsOk}, size={w}x{h} mm");
                }
                else if (!settingsOk)
                {
                    Debug.LogWarning($"[Baraonda] Impreso OK usando ajustes por defecto del driver (SetPrinterSettings falló). size={w}x{h} mm");
                }

                return ok;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Baraonda] Excepción: {ex.Message}");
                return false;
            }
        }

        // Asegura textura legible y en formato aceptado por el plugin
        private static Texture2D EnsureReadableRGBA32(Texture2D src)
        {
            if (src.format == TextureFormat.RGBA32 || src.format == TextureFormat.ARGB32 || src.format == TextureFormat.RGB24)
            {
                try { src.GetPixels32(); return src; } catch { /* no legible, se copia */ }
            }
            var c = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            c.SetPixels(src.GetPixels());
            c.Apply(false, false);
            return c;
        }

        // Helpers opcionales
        public static string[] ListPrinters()
        {
            var p = new Printer();
            int n = p.GetPrinterCount();
            var arr = new string[n];
            for (int i = 0; i < n; i++) arr[i] = p.GetPrinterName(i);
            return arr;
        }

        public static string GetDefaultPrinter()
        {
            var p = new Printer();
            return p.GetDefaultPrinterName();
        }
    }
}