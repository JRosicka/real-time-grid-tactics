using System;
using UnityEngine;

namespace Util {
    /// <summary>
    /// Provides an interface for subscribing to animation event triggers 
    /// </summary>
    public class AnimationEventListener : MonoBehaviour {
        public Action EventTriggered;
        
        public void TriggerEvent() {
            EventTriggered?.Invoke();
        }
    }
}