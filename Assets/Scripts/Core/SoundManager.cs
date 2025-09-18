using System;
using System.Collections.Generic;
using UnityEngine;

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

            // Mapear claves → clips
            _map.Clear();
            foreach (var nc in clips)
            {
                if (nc != null && !string.IsNullOrWhiteSpace(nc.key) && nc.clip != null)
                    _map[nc.key] = nc.clip;
            }

            if (sfxSource == null)
            {
                // Crear uno simple si no se asignó
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f; // 2D
            }
        }

        public void SetMuted(bool value)
        {
            muted = value;
        }

        public void SetVolume(float value01)
        {
            masterVolume = Mathf.Clamp01(value01);
        }

        /// <summary> Reproduce un clip registrado por clave. </summary>
        public void Play(string key, float volume01 = 1f, float pitch = 1f)
        {
            if (muted || sfxSource == null || string.IsNullOrWhiteSpace(key)) return;

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

        /// <summary> Reproduce un AudioClip directo (sin registrarlo). </summary>
        public void PlayClip(AudioClip clip, float volume01 = 1f, float pitch = 1f)
        {
            if (muted || sfxSource == null || clip == null) return;

            var prevPitch = sfxSource.pitch;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, masterVolume * Mathf.Clamp01(volume01));
            sfxSource.pitch = prevPitch;
        }
    }
}
