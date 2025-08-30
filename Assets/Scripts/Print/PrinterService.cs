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
        /// Intenta imprimir; si falla, guarda PNG en disco y retorna false.
        /// </summary>
        public bool TryPrint(Texture2D finalImage, out string fallbackPath)
        {
            fallbackPath = null;
            bool ok = _adapter?.Print(finalImage) == true;
            if (!ok)
                fallbackPath = FileSaver.SavePng(finalImage);
            return ok;
        }
    }
}