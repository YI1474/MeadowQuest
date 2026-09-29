using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MeadowQuest
{
    // Keep keyboard-selected rows inside the scroll viewport too.
    public sealed class ScrollSelectionIntoView : MonoBehaviour,ISelectHandler
    {
        public void OnSelect(BaseEventData data)
        {
            var scroll=GetComponentInParent<ScrollRect>(); if(!scroll) return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,transform);
            var view=scroll.viewport.rect;
            float shift=bounds.min.y<view.yMin?view.yMin-bounds.min.y:bounds.max.y>view.yMax?view.yMax-bounds.max.y:0;
            scroll.StopMovement(); scroll.content.anchoredPosition+=new Vector2(0,shift);
        }
    }
}
