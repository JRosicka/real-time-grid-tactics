using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Util {
    /// <summary>
    /// Button that triggers events when interacted with
    /// </summary>
    public class ListenerButton : Button {
        public event Action Pressed;
        public event Action NoLongerPressed;
        public event Action PressedViewLogic;
        public event Action NoLongerPressedViewLogic;
        public event Action Entered;
        public event Action Exited;
        
        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            PressedViewLogic?.Invoke();
            Pressed?.Invoke();
        }

        public void ActivateOnPointerDownView(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            PressedViewLogic?.Invoke();
            // Do not run action, just the view logic
        }

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            NoLongerPressedViewLogic?.Invoke();
            NoLongerPressed?.Invoke();
        }
        
        public void ActivateOnPointerUpView(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            NoLongerPressedViewLogic?.Invoke();
            // Do not run action, just the view logic
        }

        public override void OnPointerEnter(PointerEventData eventData) {
            base.OnPointerEnter(eventData);
            Entered?.Invoke();
        }

        public override void OnPointerExit(PointerEventData eventData) {
            base.OnPointerExit(eventData);
            Exited?.Invoke();
        }
    }
}