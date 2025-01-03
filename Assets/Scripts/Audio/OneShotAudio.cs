using System;
using UnityEngine;

namespace Audio {
	/// <summary>
	/// Runtime class that manages an audio source that will be played once and then discarded
	/// </summary>
	public class OneShotAudio {
		private readonly AudioManager _audioManager;
		protected readonly AudioSource Source;

		public bool HasSource => Source;

		public string Name => Source.gameObject.name;

		public bool IsPlaying {
			get {
				if (!Source) return false;
				return Source.isPlaying || _audioFile != null; 
			}
		}

		public bool IsBusy { get; private set; }
		
		private bool _isFading;
		private float _fadeTarget;
		private float _fadeSpeed;
		private bool _willRelease;

		private float _delayTime;
		private bool _delayStart;
		
		private float _volume;
		public float Volume {
			get => _volume;
			set {
				_isFading = false;
				_volume = value;
			}
		}


		private AudioFile _audioFile;

		public string AudioFileName {
			get {
				if (_audioFile == null) return "null";
				return _audioFile.Clip==null?"null":_audioFile.Clip.name;
			}
		}

		public AudioLayerName Layer => _audioFile?.AudioLayer ?? (AudioLayerName) (-1);
		
		/// <summary>
		/// The current position in seconds of the audio source
		/// </summary>
		public float Time => !Source ? 0 : Source.time;

		/// <summary>
		/// The length in seconds of the currently playing clip
		/// </summary>
		public float Length => !Source || !Source.clip ? 0 : Source.clip.length;
		
		public int Priority { get; private set; }

		public OneShotAudio(AudioSource audioSource, AudioManager audioManager) {
			_audioManager = audioManager;
			Source = audioSource;
			Source.minDistance = 50;
			_audioFile = null;
		}

		public void Initialize(AudioFile aFile) {
			if (!Source) throw new Exception("Trying to initialize a OneShotAudio that is missing a Source!");
			if (_audioFile != null) throw new Exception("Trying to initialize a OneShotAudio that already has an AudioFile!");
			_audioFile = aFile;
			Source.clip = _audioFile.Clip;
			Source.outputAudioMixerGroup = _audioManager.GetAudioMixerGroupForLayer(_audioFile.AudioLayer);
			_volume = 1;
			Priority = AudioManager.GetLayerPriority(_audioFile.AudioLayer);
			Source.priority = Priority;
			_willRelease = false;
			UpdateVolume();
		}

		public void Release() {
			Source.Stop();
			IsBusy = false;
			Source.clip = null;
			_willRelease = false;
			_audioFile = null;
		}

		public void Reserve() {
			IsBusy = true;
		}

		public void DoUpdate() {
			if(_audioFile == null) return;
			UpdateVolume();
			CheckRelease();
			UpdateFade();
			UpdateDelay();
			ReleaseOneShotWhenDone();
		}

		private void ReleaseOneShotWhenDone() {
			if (_audioFile == null || !Source) return;
			if (Source.loop) return;
			if (IsBusy) return;
			if (Source.isPlaying) return;
			if (_delayTime > 0) return;
			Release();
		}

		private void UpdateVolume() {
			Source.volume = _volume * _audioFile.Volume;
		}

		private void UpdateDelay() {
			if (_delayTime < 0) return;

			_delayTime -= UnityEngine.Time.deltaTime;
			if (_delayTime >= 0) return;
			
			if(_delayStart) Play();
		}

		private void CheckRelease() {
			if (_isFading) return;
			if (_delayTime >= 0) return;
			
			if (Source.loop && _willRelease) Release();
			else if (!Source.loop && !IsPlaying) Release();
			else if (IsPlaying && _willRelease && Source.volume <= 0) Release();
		}

		private void UpdateFade() {
			if (!_isFading) return;

			float result;
			var speed = _fadeSpeed * UnityEngine.Time.deltaTime;
			var diff = _fadeTarget - _volume;
			
			if (Mathf.Abs(diff) < speed) {
				result = _fadeTarget;
				_isFading = false;
			} else {
				result = _volume + speed * Mathf.Sign(diff);
			}

			_volume = result;
		}

		public void SetFade(float speed, float target) {
			if (!Source) return;
			_isFading = true;
			_fadeSpeed = Mathf.Abs(speed);
			_fadeTarget = target;
			Play();
		}

		public void Play() {
			if (!Source.isPlaying) Source.Play();
		}

		public void FadeOutAndRelease(float speed) {
			if (!Source || !Source.clip) return;
			if (Source.isPlaying) {
				SetFade(speed, 0);
				_willRelease = true;
			}
			else {
				Release();
			}
		}

		public void StopAfterDelay(float delay) {
			_willRelease = true;
			_isFading = false;
			_delayTime = delay;
			_delayStart = false;
		}

		public void StartAfterDelay(float delay) {
			_willRelease = true;
			_isFading = false;
			_delayTime = delay;
			_delayStart = true;
		}
	}
}
