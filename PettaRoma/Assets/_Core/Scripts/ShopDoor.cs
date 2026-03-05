using UnityEngine;

/// <summary>
/// Door interaction for the Petta Shop.
/// Teleports player inside/outside the shop.
/// </summary>
public class ShopDoor : Interactable
{
    [Header("Teleport Points")]
    [SerializeField] private Transform insidePoint;
    [SerializeField] private Transform outsidePoint;
    [SerializeField] private bool playerIsInside = false;

    protected override void Start()
    {
        base.Start();
        promptText = "Press E to enter Petta Shop";
    }

    protected override void OnInteract()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var pc = player.GetComponent<PlayerController>();

        if (!playerIsInside)
        {
            // Enter shop
            if (insidePoint != null && pc != null)
                pc.TeleportTo(insidePoint.position);
            promptText = "Press E to exit";
            playerIsInside = true;
            Debug.Log("[ShopDoor] Player entered Petta Shop!");
        }
        else
        {
            // Exit shop
            if (outsidePoint != null && pc != null)
                pc.TeleportTo(outsidePoint.position);
            promptText = "Press E to enter Petta Shop";
            playerIsInside = false;
            Debug.Log("[ShopDoor] Player exited Petta Shop.");
        }

        EventBus.InteractPromptShow(promptText);
    }
}
