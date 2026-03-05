using UnityEngine;

/// <summary>
/// Makes the character walk back and forth along the sidewalk.
/// Attach to the walking character GameObject.
/// </summary>
public class CharacterWalker : MonoBehaviour
{
    [Header("Walk Settings")]
    public float speed = 1.2f;          // Walking speed (m/s)
    public float walkDistance = 6f;      // Total distance to walk before turning
    public Vector3 walkDirection = Vector3.right; // Direction of walk (along X)

    private Vector3 startPos;
    private float traveled = 0f;
    private int direction = 1; // 1 = forward, -1 = backward

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += walkDirection.normalized * step * direction;
        traveled += step;

        if (traveled >= walkDistance)
        {
            traveled = 0f;
            direction *= -1;

            // Turn around: flip Y rotation by 180°
            transform.Rotate(0f, 180f, 0f);
        }
    }
}
