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
        [SerializeField] private string _textFormat = "Track: {0}";
        [SerializeField] private bool _animate;
        
        private static PlaylistManager PlaylistManager => GameAudio.Instance.PlaylistManager;

        private void Start() {
            SetText(PlaylistManager.CurrentlyPlayingTrack);
            PlaylistManager.TrackChanged += SetText;
        }
        
        private void OnDestroy() {
            PlaylistManager.TrackChanged -= SetText;
        }
        
        private void SetText(MusicAudioFile track) {
            _text.text = !track ? string.Empty : string.Format(_textFormat, track.DisplayName);
            if (_animate) {
                _animator.Play("ShowTrackInfo");
            }
        }
    }
}