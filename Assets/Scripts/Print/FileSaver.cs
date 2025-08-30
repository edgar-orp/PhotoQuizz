using System;
using System.IO;
using UnityEngine;

namespace dev.xrlab.photoquizz.Print
{
    public enum SaveTarget
    {
        Pictures,        // Imágenes del usuario (My Pictures)
        PersistentData,  // Application.persistentDataPath
        CustomPath       // Ruta base personalizada
    }

    public static class FileSaver
    {
        /// <summary>
        /// Guarda un PNG de forma silenciosa en la ruta indicada.
        /// Por defecto: Imágenes del usuario / subcarpeta (Windows: "Imágenes").
        /// Si hay error (p.ej. bloqueo del sistema), hace fallback a persistentDataPath.
        /// </summary>
        public static string SavePng(
            Texture2D tex,
            string fileNamePrefix = "print_",
            SaveTarget target = SaveTarget.Pictures,
            string subfolderName = "XRlab PhotoQuizz",
            string customBasePath = null)
        {
            if (tex == null) return null;

            try
            {
                // 1) Base path según target
                string basePath = GetBasePath(target, subfolderName, customBasePath);

                // 2) Crear carpeta si no existe
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                // 3) Nombre de archivo
                string safePrefix = SanitizeFileNameSegment(string.IsNullOrWhiteSpace(fileNamePrefix) ? "print_" : fileNamePrefix);
                string fileName = $"{safePrefix}{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = Path.Combine(basePath, fileName);

                // 4) Guardar
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaver] Error al guardar PNG: {ex.Message}");

                // Fallback silencioso: persistentDataPath
                try
                {
                    string fallbackDir = Application.persistentDataPath;
                    if (!Directory.Exists(fallbackDir))
                        Directory.CreateDirectory(fallbackDir);

                    string fileName = $"print_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string fullPath = Path.Combine(fallbackDir, fileName);
                    File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                    Debug.LogWarning($"[FileSaver] Guardado en fallback: {fullPath}");
                    return fullPath;
                }
                catch (Exception ex2)
                {
                    Debug.LogError($"[FileSaver] Fallback también falló: {ex2.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Intenta primero en Imágenes (subcarpeta) y, si falla, guarda en persistentDataPath.
        /// 100% silencioso, sin cuadros de diálogo.
        /// </summary>
        public static string SavePngPreferPicturesSilent(
            Texture2D tex,
            string fileNamePrefix = "print_",
            string subfolderName = "XRlab PhotoQuizz")
        {
            // Intento en Imágenes
            string path = SavePng(
                tex,
                fileNamePrefix,
                SaveTarget.Pictures,
                subfolderName,
                null
            );
            if (!string.IsNullOrEmpty(path)) return path;

            // Fallback a persistente
            return SavePng(
                tex,
                fileNamePrefix,
                SaveTarget.PersistentData,
                null,
                null
            );
        }

        private static string GetBasePath(SaveTarget target, string subfolderName, string customBasePath)
        {
            switch (target)
            {
                case SaveTarget.PersistentData:
                    return Application.persistentDataPath;

                case SaveTarget.CustomPath:
                    {
                        string baseCustom = string.IsNullOrWhiteSpace(customBasePath)
                            ? Application.persistentDataPath
                            : customBasePath;

                        string safeSub = SanitizeFileNameSegment(subfolderName);
                        return string.IsNullOrWhiteSpace(safeSub) ? baseCustom : Path.Combine(baseCustom, safeSub);
                    }

                case SaveTarget.Pictures:
                default:
                    {
                        // Windows/Mac/Linux: carpeta Imágenes del usuario
                        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                        if (string.IsNullOrWhiteSpace(pictures))
                            pictures = Application.persistentDataPath; // fallback improbable pero seguro

                        string safeSub = SanitizeFileNameSegment(subfolderName);
                        return string.IsNullOrWhiteSpace(safeSub) ? pictures : Path.Combine(pictures, safeSub);
                    }
            }
        }

        private static string SanitizeFileNameSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return segment;
            foreach (char c in Path.GetInvalidFileNameChars())
                segment = segment.Replace(c.ToString(), "_");
            return segment.Trim();
        }

        /// <summary>
        /// (Opcional) Abre una carpeta en el explorador de Windows. No lo llames si buscas 0 UI.
        /// </summary>
        public static void OpenFolderInExplorer(string path)
        {
#if UNITY_STANDALONE_WIN
            try
            {
                if (Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FileSaver] No se pudo abrir Explorer: {e.Message}");
            }
#endif
        }
    }
}
