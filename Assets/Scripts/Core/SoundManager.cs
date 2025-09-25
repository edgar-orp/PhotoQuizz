using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // <-- NUEVO

namespace dev.xrlab.photoquizz.Audio
{
    [Serializable]
    public class NamedClip
    {
        public string key;
        public AudioClip clip;
    }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("SFX Output")]
        [SerializeField] private AudioSource sfxSource;   // 2D, volume ~0.6–0.8

        [Header("Clips")]
        [SerializeField] private List<NamedClip> clips = new List<NamedClip>();

        private readonly Dictionary<string, AudioClip> _map = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        [Header("Options")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        public bool muted = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Rebuild(); // <-- asegura mapa + AudioSource listos

            // Re-inicializa cada vez que cargues escena (incluye tu CTRL+R)
            SceneManager.sceneLoaded += OnSceneLoaded; // <-- NUEVO
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded; // <-- NUEVO
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode mode) // <-- NUEVO
        {
            // Por si algo se perdió al recargar la escena
            Rebuild();
        }

        private void OnEnable() // <-- opcional pero útil en editor
        {
            // Si vuelves desde un dominio recargado en editor, repón dependencias
            if (Instance == this && (sfxSource == null || _map.Count == 0))
                Rebuild();
        }

        // --- Núcleo de “auto-sanación” ---
        private void Rebuild() // <-- NUEVO
        {
            // Asegura AudioSource
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f; // 2D
            }

            // Reconstruye el mapa de clips (por si se perdió referencia)
            _map.Clear();
            foreach (var nc in clips)
            {
                if (nc != null && !string.IsNullOrWhiteSpace(nc.key) && nc.clip != null)
                    _map[nc.key] = nc.clip;
            }

            // Asegura que el audio global no esté pausado
            AudioListener.pause = false;
        }

        public void SetMuted(bool value) => muted = value;
        public void SetVolume(float value01) => masterVolume = Mathf.Clamp01(value01);

        public void Play(string key, float volume01 = 1f, float pitch = 1f)
        {
            if (muted || string.IsNullOrWhiteSpace(key)) return;
            if (sfxSource == null || _map.Count == 0) Rebuild(); // <-- NUEVO safeguard

            if (_map.TryGetValue(key, out var clip) && clip != null)
            {
                var prevPitch = sfxSource.pitch;
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, masterVolume * Mathf.Clamp01(volume01));
                sfxSource.pitch = prevPitch;
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"[SoundManager] Clip no encontrado para key: {key}");
            }
#endif
        }

        public void PlayClip(AudioClip clip, float volume01 = 1f, float pitch = 1f)
        {
            if (muted || clip == null) return;
            if (sfxSource == null) Rebuild(); // <-- NUEVO safeguard

            var prevPitch = sfxSource.pitch;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, masterVolume * Mathf.Clamp01(volume01));
            sfxSource.pitch = prevPitch;
        }
    }
}
