using UnityEngine;

namespace dev.xrlab.photoquizz.Print
{
    public interface IPrinterAdapter
    {
        /// <summary>
        /// Envía la imagen a la impresora. true = enviado OK (no garantiza papel impreso).
        /// </summary>
        bool Print(Texture2D img);
    }
}