using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;

namespace Audio {
    /// <summary>
    /// Handles playing, randomizing, and managing <see cref="MusicAudioFile"/> playlists
    /// </summary>
    public class PlaylistManager {
        private readonly AudioPlayer _audioPlayer;
        private readonly AudioFileConfiguration _audioConfiguration;
        
        private List<MusicAudioFile> _currentPlaylist;
        private int _currentTrackIndex;
        private int _musicSeed = -1;
        private string _currentlyPlayingPlaylist;
        
        public event Action<MusicAudioFile> StartedPlayingTrack;

        public MusicAudioFile CurrentlyPlayingTrack {
            get {
                if (_currentPlaylist == null || _currentPlaylist.Count == 0 || _currentTrackIndex == -1) return null;
                return _currentPlaylist[_currentTrackIndex];
            }
        }
        
        public PlaylistManager(AudioPlayer audioPlayer, AudioFileConfiguration audioConfiguration) {
            _audioPlayer = audioPlayer;
            _audioConfiguration = audioConfiguration;
        }
        
        public void SetMusicSeed(int seed) {
            _musicSeed = seed;
        }
        
        public void PlayMenuMusic() {
            if (_currentlyPlayingPlaylist == nameof(_audioConfiguration.MenuMusic)) return;
            _currentlyPlayingPlaylist = nameof(_audioConfiguration.MenuMusic);
            
            PlayPlaylist(_audioConfiguration.MenuMusic, false);
        }

        public void PlayInGameMusic() {
            if (_currentlyPlayingPlaylist == nameof(_audioConfiguration.InGameMusic)) return;
            _currentlyPlayingPlaylist = nameof(_audioConfiguration.InGameMusic);

            PlayPlaylist(_audioConfiguration.InGameMusic, true);
        }
        
        public void EndMusic() {
            if (_audioPlayer.ActiveMusic != null) {
                _audioPlayer.ActiveMusic.Released -= PlayNextTrack;
            }
            _audioPlayer.EndMusic(true);
            _currentlyPlayingPlaylist = null;
        }

        private void PlayPlaylist(List<MusicAudioFile> playlist, bool useProvidedSeed) {
            // Clean up old playlist listener
            if (_audioPlayer.ActiveMusic != null) {
                _audioPlayer.ActiveMusic.Released -= PlayNextTrack;
            }

            _currentPlaylist = RandomizePlaylist(playlist, useProvidedSeed);
            _currentTrackIndex = -1;
            if (_currentPlaylist.Count > 0) {
                PlayNextTrack();
            }
        }
        
        private List<MusicAudioFile> RandomizePlaylist(List<MusicAudioFile> playlist, bool useProvidedSeed) {
            Random random = new Random(useProvidedSeed && _musicSeed != -1 ? _musicSeed : new Random().Next());
            return playlist.OrderBy(_ => random.Next()).ToList();
        }

        private void PlayNextTrack() {
            _currentTrackIndex++;
            if (_currentTrackIndex >= _currentPlaylist.Count) {
                _currentTrackIndex = 0;
            }

            OneShotAudio audioInstance = _audioPlayer.PlayMusic(_currentPlaylist[_currentTrackIndex]);
            audioInstance.Released += PlayNextTrack;

            StartedPlayingTrack?.Invoke(_currentPlaylist[_currentTrackIndex]);
        }
    }
}