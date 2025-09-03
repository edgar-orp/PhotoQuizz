namespace dev.xrlab.photoquizz.Core
{
    public enum AppState
    {
        Boot,        // Inicialización de servicios, config, etc.
        Home,        // Pantalla de inicio
        Quiz,        // Preguntas y respuestas
        Result,      // Mostrar perfil/título
        Preview,     // Preview de la cámara (sección 2, botón visible)
        Capturing,   // Cuenta regresiva y toma de fotos
        Review,      // Previsualización del mosaico
        Printing,    // Impresión en curso
        Done,        // Impresión exitosa
        Error        // Algún fallo
    }
}