using System.Collections.Generic;
using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class BellRingerSpatialSoundTarget : MonoBehaviour
    {
        private static readonly List<BellRingerSpatialSoundTarget> ActiveTargetList = new List<BellRingerSpatialSoundTarget>(16);

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool enforceRecommended3dSettings = true;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loopAudio = true;
        [SerializeField] private float recommendedMinDistance = 1f;
        [SerializeField] private float recommendedMaxDistance = 8f;
        [SerializeField] private float ledIntensityMultiplier = 1f;

        public static IReadOnlyList<BellRingerSpatialSoundTarget> ActiveTargets => ActiveTargetList;

        public AudioSource Source => audioSource;
        public float LedIntensityMultiplier => ledIntensityMultiplier;

        private void Awake()
        {
            ResolveAudioSource();
            ApplyRecommendedSettings();
        }

        private void OnEnable()
        {
            ResolveAudioSource();
            ApplyRecommendedSettings();

            if (!ActiveTargetList.Contains(this))
            {
                ActiveTargetList.Add(this);
            }
        }

        private void Start()
        {
            if (playOnStart && audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void OnDisable()
        {
            ActiveTargetList.Remove(this);
        }

        private void OnValidate()
        {
            ResolveAudioSource();
            ApplyRecommendedSettings();
        }

        private void OnDrawGizmosSelected()
        {
            if (audioSource == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            Gizmos.DrawSphere(transform.position, 0.15f);
            Gizmos.DrawWireSphere(transform.position, audioSource.maxDistance);
        }

        private void ResolveAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void ApplyRecommendedSettings()
        {
            if (!enforceRecommended3dSettings || audioSource == null)
            {
                return;
            }

            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = recommendedMinDistance;
            audioSource.maxDistance = Mathf.Max(recommendedMaxDistance, recommendedMinDistance + 0.01f);
            audioSource.loop = loopAudio;
            audioSource.playOnAwake = playOnStart;
        }
    }
}
