using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Settings menu that appears in-game and in the main menu
    /// </summary>
    public class SettingsMenu : MonoBehaviour {
        [Header("Config")] 
        [Tooltip("Whether this is a settings menu instance that appears in-game")]
        [SerializeField] private bool _inGame;
        [SerializeField] private float _descriptionFadeSeconds = .25f;

        [Header("References")]
        [SerializeField] private List<SettingSubMenuButton> _settingSubMenuButtons;
        [SerializeField] private SettingSubMenuButton _exitButton;
        [SerializeField] private Transform _settingSubMenuVerticalLine;
        [SerializeField] private List<SettingEntry> _settingEntries;
        [SerializeField] private Transform _settingEntryVerticalLine;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private CanvasGroup _descriptionCanvasGroup;
        
        [Header("Settings fields")]
        [SerializeField] private SettingDropdownList _rightClickBehaviorList;
        [SerializeField] private SettingDropdownList _targetCommandBehaviorList;
        [SerializeField] private SettingSlider _masterVolumeSlider;
        [SerializeField] private SettingSlider _sfxVolumeSlider;
        [SerializeField] private SettingSlider _voiceLineVolumeSlider;
        [SerializeField] private SettingSlider _musicVolumeSlider;
        [SerializeField] private SettingToggle _lockCursorToggle;
        [SerializeField] private SettingToggle _edgeScrollToggle;
        [SerializeField] private SettingSlider _edgeScrollSpeedSlider;
        [SerializeField] private SettingSlider _menuUIScalingSlider;
        [SerializeField] private SettingSlider _inGameUIScalingSlider;
        [SerializeField] private SettingDropdownList _displayList;

        private Action _onDismiss;
        private bool _descriptionTextEnabled;
        
        public bool Active { get; private set; }
        
        // null if not in game
        private GameManager GameManager => GameManager.Instance;
        
        private void Start() {
            InitializeMenu();
            InitializeSettingsFields();
        }

        private void Update() {
            if ((_descriptionCanvasGroup.alpha <= 0 && !_descriptionTextEnabled) ||
                (_descriptionCanvasGroup.alpha >= 1 && _descriptionTextEnabled)) {
                return;
            }
            
            float sign = _descriptionTextEnabled ? 1 : -1;
            _descriptionCanvasGroup.alpha += sign * (Time.deltaTime / _descriptionFadeSeconds);
            _descriptionCanvasGroup.alpha = Mathf.Clamp01(_descriptionCanvasGroup.alpha);
        }

        public void Open(Action onDismiss) {
            _onDismiss = onDismiss;
            gameObject.SetActive(true);
            Active = true;
        }

        public void Close() {
            gameObject.SetActive(false);
            Active = false;
            _onDismiss?.Invoke();
            _onDismiss = null;
        }
        
        #region Menu View/UI

        private void InitializeMenu() {
            // Button events
            foreach (SettingSubMenuButton button in _settingSubMenuButtons) {
                button.Initialize();
                button.Entered += SwitchSubMenu;
            }
            
            // Initial button state
            SwitchSubMenu(_settingSubMenuButtons[0]);
            
            // Entry events
            foreach (SettingEntry setting in _settingEntries) {
                setting.Initialize();
                setting.Entered += SwitchHoveredSetting;
            }
        }

        private void SwitchSubMenu(SettingSubMenuButton selectedButton) {
            _settingSubMenuVerticalLine.position = new Vector2(_settingSubMenuVerticalLine.position.x, selectedButton.HeightWorldPosition);
            _settingEntryVerticalLine.gameObject.SetActive(false);
            
            ToggleDescriptionText(string.Empty);
            
            // Don't switch submenus if we are hovering over the exit button
            if (selectedButton != _exitButton) {
                foreach (SettingSubMenuButton button in _settingSubMenuButtons) {
                    bool selected = button == selectedButton;
                    button.SetSelected(selected);
                }
            }
        }

        private void SwitchHoveredSetting(SettingEntry settingEntry) {
            _settingEntryVerticalLine.gameObject.SetActive(true);
            _settingEntryVerticalLine.position = new Vector2(_settingEntryVerticalLine.position.x, settingEntry.HeightWorldPosition);
            ToggleDescriptionText(settingEntry.Description);
        }

        private void ToggleDescriptionText(string text) {
            _descriptionText.text = text;
            _descriptionTextEnabled = text != string.Empty;
        }
        
        #endregion
        #region Individual Settings

        private void InitializeSettingsFields() {
            // 0 - 100
            int rightClickBehavior = PlayerPrefs.GetInt(PlayerPrefsKeys.RightClickBehaviorKey, 0);
            int targetCommandBehavior = PlayerPrefs.GetInt(PlayerPrefsKeys.TargetCommandBehaviorKey, 0);
            int masterVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.MasterVolumeKey, PlayerPrefsKeys.DefaultVolume));
            int sfxVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.SoundEffectVolumeKey, PlayerPrefsKeys.DefaultVolume));
            int voiceLineVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.VoiceLineVolumeKey, PlayerPrefsKeys.DefaultVolume));
            int musicVolume = ToVolumeInt(PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolumeKey, PlayerPrefsKeys.DefaultVolume));
            bool lockCursor = PlayerPrefs.GetInt(PlayerPrefsKeys.LockCursorKey, 1) == 1;
            bool edgeScroll = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollKey, 1) == 1;
            int edgeScrollSpeed = PlayerPrefs.GetInt(PlayerPrefsKeys.EdgeScrollSpeed, PlayerPrefsKeys.DefaultEdgeScrollSpeed);
            int uiScaleMenu = PlayerPrefs.GetInt(PlayerPrefsKeys.UIScaleMenu, PlayerPrefsKeys.DefaultUIScale);
            int uiScaleInGame = PlayerPrefs.GetInt(PlayerPrefsKeys.UIScaleInGame, PlayerPrefsKeys.DefaultUIScale);
            int chosenDisplay = PlayerPrefs.GetInt(PlayerPrefsKeys.ChosenDisplayKey, 0);
            
            _rightClickBehaviorList.Initialize(rightClickBehavior, new List<string> {"Attack", "Move"});
            _rightClickBehaviorList.ValueChanged += RightClickBehaviorChanged;

            _targetCommandBehaviorList.Initialize(targetCommandBehavior, new List<string> {"Left Click", "Left/Right Click"});
            _targetCommandBehaviorList.ValueChanged += TargetCommandBehaviorChanged;

            _masterVolumeSlider.Initialize(masterVolume);
            _masterVolumeSlider.ValueChanged += MasterVolumeChanged;

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
            
            _menuUIScalingSlider.Initialize(uiScaleMenu);
            _menuUIScalingSlider.ValueChanged += MenuUIScaleChanged;

            _inGameUIScalingSlider.Initialize(uiScaleInGame);
            _inGameUIScalingSlider.ValueChanged += InGameUIScaleChanged;

            List<string> displayStrings = Display.displays.Take(8).Select((_, i) => $"Display {i + 1}").ToList();
            _displayList.Initialize(chosenDisplay, displayStrings);
            _displayList.ValueChanged += ChosenDisplayChanged;
        }

        private void RightClickBehaviorChanged(int newSetting) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.RightClickBehaviorKey, newSetting);
        }

        private void TargetCommandBehaviorChanged(int newSetting) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.TargetCommandBehaviorKey, newSetting); 
        }

        private static void MasterVolumeChanged(int volume) {
            float masterVolume = ToPersistedVolumeFloat(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.MasterVolumeKey, masterVolume);
            AudioManager.Instance.SetMasterVolume(masterVolume);
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
        
        private static int ToVolumeInt(float volume) {
            return (int) (volume * 100);
        }
        private static float ToPersistedVolumeFloat(int volume) {
            return volume / 100f;
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

        private void MenuUIScaleChanged(int scale) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.UIScaleMenu, scale);
            if (!_inGame) {
                MultiplayerMenu.Instance?.UIScaler.SetScale(scale);
                RoomMenu.Instance?.UIScaler.SetScale(scale);
            }
        }

        private void InGameUIScaleChanged(int scale) {
            PlayerPrefs.SetInt(PlayerPrefsKeys.UIScaleInGame, scale);
            if (_inGame) {
                GameManager.UIScaler.SetScale(scale);
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

        #endregion
    }
}