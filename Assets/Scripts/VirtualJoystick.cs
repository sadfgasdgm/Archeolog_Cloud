using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;

    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, e.position, e.pressEventCamera, out Vector2 pos);
        Vector2 half = background.sizeDelta / 2f;
        Direction = Vector2.ClampMagnitude(new Vector2(pos.x / half.x, pos.y / half.y), 1f);
        handle.anchoredPosition = Vector2.Scale(Direction, half);
    }

    public void OnPointerUp(PointerEventData e)
    {
        Direction = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}