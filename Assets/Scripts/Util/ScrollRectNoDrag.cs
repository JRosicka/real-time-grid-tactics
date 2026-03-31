using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Util {
    /// <summary>
    /// A scroll rect that does not allow click-to-drag
    /// </summary>
    public class ScrollRectNoDrag : ScrollRect {
        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }
    }
}