using UnityEngine;
using PrintLib;

namespace dev.xrlab.photoquizz.Print
{
    /// Imprime una Texture2D a 4x6" (152x102 mm) sin diálogos.
    public class BaraondaPrinterAdapter : IPrinterAdapter
    {
        public Orientation orientation = Orientation.Landscape; // o Portrait
        public float pageWidthMM = 152f;  // 6"
        public float pageHeightMM = 102f;  // 4"
        public int dpi = 300;
        public ColorMode colorMode = ColorMode.Color;
        public int copies = 1;
        public DuplexMode duplex = DuplexMode.Default;  // o (DuplexMode)0
        public string preferredPrinterName = null;      // null => impresora predeterminada

        public bool Print(Texture2D img)
        {
            if (img == null) return false;

            try
            {
                Texture2D printable = EnsureReadableRGBA32(img);
                var printer = new Printer();

                // (Opcional) seleccionar impresora específica
                if (!string.IsNullOrEmpty(preferredPrinterName))
                    printer.SelectPrinter(preferredPrinterName);

                // Configurar página personalizada (mm)
                printer.SetPrinterSettings(
                    orientation,
                    pageWidthMM,
                    pageHeightMM,
                    dpi,
                    colorMode,
                    copies,
                    duplex
                );

                // SOLO este método existe en tu versión:
                float wmm = 0f, hmm = 0f;
                try { printer.GetPageSize(ref wmm, ref hmm); } catch { /* algunas builds no lo exponen; continuar */ }

                // Documento
                printer.StartDocument();
                printer.SetPrintPosition(0f, 0f); // origen en mm (arriba-izquierda)
                int rc = printer.PrintTexture(printable, pageWidthMM, pageHeightMM);
                printer.EndDocument();

                string gdi = printer.GetLastGdiStatus();
                bool ok = (rc == 0) && string.IsNullOrEmpty(gdi);
                if (!ok)
                    Debug.LogError($"[Baraonda] Error al imprimir. rc={rc}, GDI='{gdi}', page={wmm}x{hmm} mm");
                return ok;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Baraonda] Excepción: {ex.Message}");
                return false;
            }
        }

        private static Texture2D EnsureReadableRGBA32(Texture2D src)
        {
            // Si ya es RGB24/RGBA32 y legible, úsala tal cual
            if (src.format == TextureFormat.RGBA32 || src.format == TextureFormat.ARGB32 || src.format == TextureFormat.RGB24)
            {
                try { src.GetPixels32(); return src; } catch { /* no legible */ }
            }
            // Copiar a RGBA32 legible
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
