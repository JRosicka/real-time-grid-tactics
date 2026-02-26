using Audio;
using TMPro;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// Displays the currently playing music track from <see cref="PlaylistManager"/>
    /// </summary>
    public class MusicTrackDisplayer : MonoBehaviour {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Animator _animator;
        
        private static PlaylistManager PlaylistManager => GameAudio.Instance.PlaylistManager;

        private void Start() {
            SetText(PlaylistManager.CurrentlyPlayingTrack);
            PlaylistManager.StartedPlayingTrack += SetText;
        }
        
        private void OnDestroy() {
            PlaylistManager.StartedPlayingTrack -= SetText;
        }
        
        private void SetText(MusicAudioFile track) {
            _text.text = track?.DisplayName ?? string.Empty;
            _animator.Play("ShowTrackInfo");
        }
    }
}