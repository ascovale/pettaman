using UnityEngine;

/// <summary>
/// Rotating, bobbing collectible coin.
/// Attach to a small primitive (cylinder scaled flat, or sphere) with collider set to isTrigger.
/// Destroys itself on pickup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.25f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // Spin around Y
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Bob up-down
        Vector3 pos = _startPos;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null)
            GameManager.Instance.AddCoins(value);

        // TODO Phase 2 — play coin SFX via AudioManager
        Destroy(gameObject);
    }
}
