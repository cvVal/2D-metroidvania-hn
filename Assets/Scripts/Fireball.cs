using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float m_damage;
    [SerializeField] private float m_hitForce;
    [SerializeField] private float m_speed;
    [SerializeField] private float m_lifetime = 1f;
    private bool m_hit;

    void Start()
    {
        // Auto clean-up after lifetime if it doesn't hit anything
        Destroy(gameObject, m_lifetime);
    }

    void FixedUpdate()
    {
        if (m_hit) return; // stop moving once it has hit
        transform.position += (Vector3)(m_speed * Time.fixedDeltaTime * transform.right);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (m_hit) return;
        if (!other.CompareTag("Enemy")) return;

        if (other.TryGetComponent<EnemyController>(out var enemy))
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            enemy.EnemyHit(m_damage, dir, -m_hitForce);
        }

        m_hit = true;
        // Immediate visual disappearance
        gameObject.SetActive(false); // hides now (before end of frame)
        Destroy(gameObject); // destroy at end of frame
    }
}
