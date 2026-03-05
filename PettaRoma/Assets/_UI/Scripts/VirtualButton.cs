using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Virtual button for mobile controls.
/// Set the ButtonAction in the Inspector: Jump, Attack, Interact, Sprint.
///
/// Jump/Attack/Interact trigger on press (one-shot).
/// Sprint is held (continuous).
/// </summary>
public enum ButtonAction { Jump, Attack, Interact, Sprint }

public class VirtualButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ButtonAction action;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (InputManager.Instance == null) return;

        switch (action)
        {
            case ButtonAction.Jump:     InputManager.Instance.TriggerJump(); break;
            case ButtonAction.Attack:   InputManager.Instance.TriggerAttack(); break;
            case ButtonAction.Interact: InputManager.Instance.TriggerInteract(); break;
            case ButtonAction.Sprint:   InputManager.Instance.SetSprint(true); break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (InputManager.Instance == null) return;

        if (action == ButtonAction.Sprint)
            InputManager.Instance.SetSprint(false);
    }
}
