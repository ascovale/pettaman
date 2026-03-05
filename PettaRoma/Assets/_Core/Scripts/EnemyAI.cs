using UnityEngine;

/// <summary>
/// Simple enemy AI that patrols and chases the player.
/// The "angry pizza" enemy: patrol between waypoints,
/// chase player when in range, deal damage on contact.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Dead }

    [Header("Stats")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float maxHealth = 50f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointThreshold = 1f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    // ── Internal ──
    private CharacterController cc;
    private EnemyState state = EnemyState.Patrol;
    private Transform player;
    private int currentPatrolIndex = 0;
    private float attackTimer;
    private float health;
    private float gravity = -20f;
    private float verticalVelocity;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public EnemyState CurrentState => state;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        health = maxHealth;

        // Find player
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) player = playerGo.transform;
    }

    void Update()
    {
        if (state == EnemyState.Dead) return;
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // State transitions
        switch (state)
        {
            case EnemyState.Patrol:
                if (distToPlayer < detectionRange)
                    state = EnemyState.Chase;
                break;
            case EnemyState.Chase:
                if (distToPlayer > detectionRange * 1.5f)
                    state = EnemyState.Patrol;
                else if (distToPlayer < attackRange)
                    state = EnemyState.Attack;
                break;
            case EnemyState.Attack:
                if (distToPlayer > attackRange * 1.5f)
                    state = EnemyState.Chase;
                break;
        }

        // Execute state
        switch (state)
        {
            case EnemyState.Patrol: DoPatrol(); break;
            case EnemyState.Chase: DoChase(); break;
            case EnemyState.Attack: DoAttack(); break;
        }

        // Apply gravity
        if (cc.isGrounded) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;
    }

    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // No patrol points — just idle / wander in small circle
            float angle = Time.time * 0.5f;
            Vector3 wanderDir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            Move(wanderDir, patrolSpeed * 0.3f);
            return;
        }

        Transform target = patrolPoints[currentPatrolIndex];
        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        if (dir.magnitude < waypointThreshold)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            return;
        }

        Move(dir.normalized, patrolSpeed);
    }

    void DoChase()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        Move(dir.normalized, chaseSpeed);
    }

    void DoAttack()
    {
        // Face player
        FaceDirection(player.position - transform.position);

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            // Deal damage to player
            EventBus.HealthChanged(
                Mathf.Max(0, 100f - attackDamage), 100f); // simplified
            Debug.Log($"[Enemy] Pizza attacks! Damage: {attackDamage}");
        }
    }

    void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 move = direction * speed;
        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);

        FaceDirection(direction);
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        Transform rotTarget = visualRoot != null ? visualRoot : transform;
        rotTarget.rotation = Quaternion.RotateTowards(
            rotTarget.rotation, targetRot, 360f * Time.deltaTime);
    }

    /// <summary>Called by PlayerController or weapon system.</summary>
    public void TakeDamage(float amount)
    {
        if (state == EnemyState.Dead) return;
        health -= amount;
        Debug.Log($"[Enemy] Took {amount} damage. HP: {health}/{maxHealth}");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        state = EnemyState.Dead;
        EventBus.EnemyDied();
        Debug.Log("[Enemy] Pizza enemy defeated!");

        // Shrink and disable after delay
        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        // Shrink over 0.5s
        Vector3 origScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            transform.localScale = Vector3.Lerp(origScale, Vector3.zero, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
