namespace dev.xrlab.photoquizz.Core
{
    public enum AppState
    {
        Boot,       // Inicialización de servicios, config, etc.
        Home,       // Pantalla de inicio
        Quiz,       // Preguntas y respuestas
        Result,     // Mostrar perfil/título
        Photo,      // Secuencia de fotos (countdown + captura)
        Review,     // Previsualización del mosaico
        Printing,   // Impresión en curso
        Done,       // Impresión exitosa
        Error       // Algún fallo
    }
}