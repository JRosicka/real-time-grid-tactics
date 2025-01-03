using UnityEngine;

namespace Audio {
	/// <summary>
	/// Manages an audio source that will be looping and provides for being kept in a pool in AudioManager
	/// </summary>
	public class LoopingAudio : OneShotAudio {
		public LoopingAudio(AudioSource audioSource, AudioManager audioManager) : base(audioSource, audioManager) {
			Source.loop = true;
		}
	}
}
