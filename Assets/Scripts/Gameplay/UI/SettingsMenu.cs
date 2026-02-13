using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Settings menu that appears in-game and in the main menu
    /// </summary>
    public class SettingsMenu : MonoBehaviour {
        // private readonly List<string> _resourcesCornerLocations = new List<string> {
        //     "Bottom Left",
        //     "Top Left",
        //     "Top Right",
        // };
        
        [SerializeField] private SettingSlider _sfxVolumeSlider;
        [SerializeField] private SettingSlider _voiceLineVolumeSlider;
        [SerializeField] private SettingSlider _musicVolumeSlider;
        [SerializeField] private SettingToggle _lockCursorToggle;
        [SerializeField] private SettingToggle _edgeScrollToggle;
        [SerializeField] private SettingSlider _edgeScrollSpeedSlider;
        [SerializeField] private SettingSlider _edgeScrollSpeedSensitivitySlider;
        [SerializeField] private SettingDropdownList _displayList;
        [SerializeField] private SettingDropdownList _resourcesUIList;

        [Tooltip("Whether this is a settings menu instance that appears in-game")]
        [SerializeField] private bool _inGame;

        private Action _onDismiss;
        
        public bool Active { get; private set; }
        
        // null if not in game
        private GameManager GameManager => GameManager.Instance;
        
        private void Start() {
            // 0 - 100
            int sfxVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.SoundEffectVolumeKey, PlayerPrefsKeys.DefaultVolume));
            int voiceLineVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.VoiceLineVolumeKey, PlayerPrefsKeys.DefaultVolume));
            int musicVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolumeKey, PlayerPrefsKeys.DefaultVolume));
            bool lockCursor = PlayerPrefs.GetInt(PlayerPrefsKeys.LockCursorKey, 1) == 1;
            bool edgeScroll = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollKey, 1) == 1;
            int edgeScrollSpeed = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSpeed, PlayerPrefsKeys.DefaultEdgeScrollSpeed);
            int edgeScrollSensitivity = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSensitivity, PlayerPrefsKeys.DefaultEdgeScrollSensitivity);
            int chosenDisplay = PlayerPrefs.GetInt(PlayerPrefsKeys.ChosenDisplayKey, 0);
            // _resourcesUILocation = PlayerPrefs.GetInt(PlayerPrefsKeys.ResourcesUILocationKey, 0);
            
            _sfxVolumeSlider.Initialize(sfxVolume);
            _sfxVolumeSlider.ValueChanged += SFXVolumeChanged;

            _voiceLineVolumeSlider.Initialize(voiceLineVolume);
            _voiceLineVolumeSlider.ValueChanged += VoiceLineVolumeChanged;

            _musicVolumeSlider.Initialize(musicVolume);
            _musicVolumeSlider.ValueChanged += MusicVolumeChanged;
            
            _lockCursorToggle.Initialize(lockCursor);
            _lockCursorToggle.ValueChanged += LockCursorChanged;
            
            _edgeScrollToggle.Initialize(edgeScroll);
            _edgeScrollToggle.ValueChanged += EdgeScrollChanged;
            
            _edgeScrollSpeedSlider.Initialize(edgeScrollSpeed);
            _edgeScrollSpeedSlider.ValueChanged += EdgeScrollSpeedChanged;

            _edgeScrollSpeedSensitivitySlider.Initialize(edgeScrollSensitivity);
            _edgeScrollSpeedSensitivitySlider.ValueChanged += EdgeScrollSensitivityChanged;

            List<string> displayStrings = Display.displays.Take(8).Select((d, i) => $"Display {i + 1}").ToList();
            _displayList.Initialize(chosenDisplay, displayStrings);
            _displayList.ValueChanged += ChosenDisplayChanged;
            
            // _resourcesUIList.Initialize(_resourcesUILocation, _resourcesCornerLocations);
            // _resourcesUIList.ValueChanged += ResourcesUILocationChanged;
        }

        private static void SFXVolumeChanged(int volume) {
            float sfxVolume = ToPersistedVolumeFloat(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.SoundEffectVolumeKey, sfxVolume);
            AudioManager.Instance.SetSoundEffectVolume(sfxVolume);
        }
        
        private static void VoiceLineVolumeChanged(int volume) {
            float voiceLineVolume = ToPersistedVolumeFloat(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.VoiceLineVolumeKey, voiceLineVolume);
            AudioManager.Instance.SetVoiceLineVolume(voiceLineVolume);
        }

        private static void MusicVolumeChanged(int volume) {
            float sfxVolume = ToPersistedVolumeFloat(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.MusicVolumeKey, sfxVolume);
            AudioManager.Instance.SetMusicVolume(sfxVolume);
        }

        private void LockCursorChanged(bool lockCursor) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.LockCursorKey, lockCursor ? 1 : 0);
            
            #if !UNITY_EDITOR
            Cursor.lockState = lockCursor ? CursorLockMode.Confined : CursorLockMode.None;
            #endif
        }
        
        private void EdgeScrollChanged(bool edgeScroll) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.EdgeScrollKey, edgeScroll ? 1 : 0);
            if (_inGame) {
                GameManager.CameraManager.ToggleEdgeScroll(edgeScroll);
            }
        }
        
        private void EdgeScrollSpeedChanged(int speed) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.EdgeScrollSpeed, speed);
            if (_inGame) {
                GameManager.CameraManager.SetEdgeScrollSpeed(speed);
            }
        }

        private void EdgeScrollSensitivityChanged(int sensitivity) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.EdgeScrollSensitivity, sensitivity);
            if (_inGame) {
                GameManager.CameraManager.SetEdgeScrollSensitivity(sensitivity);
            }
        }

        private void ChosenDisplayChanged(int display) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.ChosenDisplayKey, display);
            if (Camera.main != null) {
                List<DisplayInfo> displays = new List<DisplayInfo>();
                Screen.GetDisplayLayout(displays);
                Screen.MoveMainWindowTo(displays[display], new Vector2Int(0, 0));
            }
        }

        // private void ResourcesUILocationChanged(int location) {
        //     PlayerPrefs.SetInt(PlayerPrefsKeys.ResourcesUILocationKey, location);
        //     if (_inGame) {
        //         // TODO
        //         
        //     }
        // }

        public void Open(Action onDismiss) {
            _onDismiss = onDismiss;
            gameObject.SetActive(true);
            Active = true;
        }

        public void Close() {
            gameObject.SetActive(false);
            _resourcesUIList.DismissList();
            Active = false;
            _onDismiss?.Invoke();
            _onDismiss = null;
        }
        
        private static int ToVolumeInt(float volume) {
            return (int) (volume * 100);
        }
        private static float ToPersistedVolumeFloat(int volume) {
            return volume / 100f;
        }
    }
}