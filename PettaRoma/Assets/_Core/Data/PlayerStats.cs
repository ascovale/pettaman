using UnityEngine;

/// <summary>
/// ScriptableObject holding player stats.
/// Create via: Assets > Create > Petta > PlayerStats
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Petta/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float jumpForce = 9f;
    public float gravity = -22f;
    public float rotationSpeed = 720f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.3f;
    public float groundCheckOffset = 0.05f;

    [Header("Combat — Phase 3")]
    public float maxHealth = 100f;
    public float attackDamage = 25f;
    public float attackRange = 1.8f;
    public float attackCooldown = 0.4f;
}
