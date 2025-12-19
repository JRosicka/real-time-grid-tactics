using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Util;

namespace Audio {
    /// <summary>
    /// Handles playing and canceling audio instances, and keeping track of audio state. Also handles creating and moving
    /// audio objects to an audio scene. 
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class AudioManager : MonoBehaviour {
        // Loudness (perceptually) ~doubles every 10 decibels -> 10/ln(2)
        private const float DecibelCoefficient = 14.4269504089f;

        private const float GlobalVolumeMultiplier = 2.5f;
        
        public static AudioManager Instance;

#if UNITY_EDITOR
        static AudioManager() {
            EditorApplication.playModeStateChanged += ResetStaticFields;
        }
        private static void ResetStaticFields(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                Instance = null;
            }
        }
#endif

        private int _sourceCount;
        private readonly List<OneShotAudio> _bufferOneShot = new List<OneShotAudio>();
        private readonly List<OneShotAudio> _bufferUnInterruptibleOneShot = new List<OneShotAudio>();
        private readonly List<LoopingAudio> _bufferLooping = new List<LoopingAudio>();
        private Transform _listener;

        private AudioMixer _mixer;
        private AudioMixer Mixer {
            get => _mixer;
            set {
                _mixer = value;
                _layerMixerGroups = EnumUtil.GetValues<AudioLayerName>().ToDictionary(e => e, e => _mixer.
                    FindMatchingGroups(e.ToString()).FirstOrDefault());
            }
        }
        private Dictionary<AudioLayerName, AudioMixerGroup> _layerMixerGroups;
        
        public void Initialize() {
            Instance = this;
            Mixer = Resources.Load<AudioMixer>("Mixer");
        }
        
        /// <summary>
        /// Tell the audio objects to update
        /// </summary>
        private void Update() {
            DoUpdate(_bufferOneShot);
            DoUpdate(_bufferUnInterruptibleOneShot);
            DoUpdate(_bufferLooping);
        }

        /// <summary>
        /// Loop through the audio objects in the set and cause them to update their state.
        /// </summary>
        private static void DoUpdate<T>(IList<T> list) where T : OneShotAudio {
            if (list == null) return;
            
            //Don't create garbage
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < list.Count; i++) {
                T source = list[i];
                if (source == null) continue;
                source.DoUpdate();
            }
        }
        
        /// <summary>
        /// create a new game object and move it to the audio scene
        /// </summary>
        private GameObject CreateGameObject(string gameObjectName) {
            GameObject go = new GameObject(gameObjectName);
            
            // Move to this gameObject so that it doesn't get destroyed
            go.transform.parent = transform;
            return go;
        }
        
        /// <summary>
        /// Find the mixer group for a particular layer
        /// </summary>
        public AudioMixerGroup GetAudioMixerGroupForLayer(AudioLayerName layer) {
            return _layerMixerGroups.ContainsKey(layer) ? _layerMixerGroups[layer] : null;
        }
        
        public OneShotAudio PlaySound(AudioFile audioFile, bool loop, bool interruptible) {
            if (audioFile == null || !audioFile.Clip) {
                Debug.LogError("Trying to play a null AudioClip");
                return null;
            }

            if (Instance == null) {
                Debug.LogWarning("Make sure to initialize the Audio Manager before trying to play sounds with it!");
                return null;
            }

            OneShotAudio audioInstance = loop
                ? GetLoopingAudioSource()
                : interruptible
                    ? GetOneShotAudioSource()
                    : GetUnInterruptibleOneShotAudioSource();
            audioInstance.Initialize(audioFile);
            audioInstance.Play();
            return audioInstance;
        }

        /// <summary>
        /// retrieve an audio object that can play a looping audio file.
        /// </summary>
        private LoopingAudio GetLoopingAudioSource() {
            LoopingAudio loopingAudio = _bufferLooping.FirstOrDefault(next => !next.IsBusy);

            if (loopingAudio == null) {
                loopingAudio = new LoopingAudio(MakeNewAudioSource(), this);
                _bufferLooping.Add(loopingAudio);
            }

            loopingAudio.Reserve();
            return loopingAudio;
        }

        /// <summary>
        /// retrieve an audio object that can play an audio file once
        /// </summary>
        private OneShotAudio GetOneShotAudioSource() {
            OneShotAudio oneShot = _bufferOneShot.FirstOrDefault(next => !next.IsPlaying && !next.IsBusy);

            if (oneShot == null) {
                oneShot = new OneShotAudio(MakeNewAudioSource(), this);
                _bufferOneShot.Add(oneShot);
            }

            return oneShot;
        }

        private OneShotAudio GetUnInterruptibleOneShotAudioSource() {
            OneShotAudio oneShot = _bufferUnInterruptibleOneShot.FirstOrDefault(next => !next.IsPlaying && !next.IsBusy);

            if (oneShot == null) {
                oneShot = new OneShotAudio(MakeNewAudioSource(), this);
                _bufferUnInterruptibleOneShot.Add(oneShot);
            }

            return oneShot;
        }

        /// <summary>
        /// make a new audio source object in the audio scene
        /// </summary>
        private AudioSource MakeNewAudioSource() {
            GameObject go = CreateGameObject($"{nameof(AudioSource)}_{_sourceCount++}");
            AudioSource source = go.AddComponent<AudioSource>();
			
            source.playOnAwake = false;
            source.loop = false;
            return source;
        }

        public void CancelAudio(OneShotAudio audioInstance, bool fadeOutFirst, float fadeSpeed = 1f) {
            if (fadeOutFirst) {
                audioInstance.FadeOutAndRelease(fadeSpeed);
            } else {
                audioInstance.Release();
            }
        }

        public void SetSoundEffectVolume(float newVolume) {
            Mixer.SetFloat(PlayerPrefsKeys.SoundEffectVolumeKey, ToDecibels(newVolume * GlobalVolumeMultiplier));
        }
        
        public void SetMusicVolume(float newVolume) {
            Mixer.SetFloat(PlayerPrefsKeys.MusicVolumeKey, ToDecibels(newVolume * GlobalVolumeMultiplier));
        }

        /// <summary> 
        /// Convert from a perceptually linear scale.
        /// </summary>
        private static float ToDecibels(float value) {
            return Mathf.Max(-80, DecibelCoefficient*Mathf.Log(value));
        }
        
        /// <summary>
        /// find the audio source priorities for a given layer
        /// </summary>
        public static int GetLayerPriority(AudioLayerName layer) {
            switch (layer) {
                case AudioLayerName.SoundEffects:
                    return 200;
                case AudioLayerName.Music:
                    return 100;
                case AudioLayerName.Undefined:
                    return -1;
                default:
                    Debug.LogError("Unknown layer requested: " + layer);
                    break;
            }

            return 200;
        }
    }
}