using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile virtual joystick.
/// Attach to the joystick Background image (UI).
/// The Handle image should be a child of the Background.
///
/// Setup:
///   Canvas (Screen Space - Overlay)
///     └── JoystickBG (Image, this script)
///           └── JoystickHandle (Image)
/// </summary>
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private float deadZone = 0.1f;

    private Vector2 inputVector;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPos);

        // Normalize to -1..1 based on background size
        Vector2 halfSize = background.sizeDelta * 0.5f;
        inputVector = new Vector2(localPos.x / halfSize.x, localPos.y / halfSize.y);
        inputVector = Vector2.ClampMagnitude(inputVector, 1f);

        // Apply dead zone
        if (inputVector.magnitude < deadZone)
            inputVector = Vector2.zero;

        // Move handle visual
        handle.anchoredPosition = inputVector * handleRange;

        // Send to InputManager
        if (InputManager.Instance != null)
            InputManager.Instance.SetVirtualMove(inputVector);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        if (InputManager.Instance != null)
            InputManager.Instance.SetVirtualMove(Vector2.zero);
    }
}
