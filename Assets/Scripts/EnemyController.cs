using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] protected float m_health;
    [SerializeField] protected float m_recoilLength;
    [SerializeField] protected float m_recoilFactor;
    [SerializeField] protected bool m_isRecoiling = false;

    [SerializeField] protected float m_speed;
    [SerializeField] protected float m_damage;

    protected float m_recoilTimer = 0f;
    protected Rigidbody2D m_rigidbody2D;

    protected virtual void Awake()
    {
        m_rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (m_health <= 0)
        {
            Die();
        }

        if (m_isRecoiling)
        {
            if (m_recoilTimer < m_recoilLength)
            {
                m_recoilTimer += Time.deltaTime;
            }
            else
            {
                m_isRecoiling = false;
                m_recoilTimer = 0f;
            }
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public virtual void EnemyHit(float _damage, Vector2 _hitDirection, float _hitForce)
    {
        m_health -= _damage;
        if (!m_isRecoiling)
        {
            m_rigidbody2D.AddForce(-_hitForce * m_recoilFactor * _hitDirection);
            m_isRecoiling = true;
        }
    }

    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(m_damage);
    }

    protected void OnCollisionStay2D(Collision2D _other)
    {
        if (_other.gameObject.CompareTag("Player") && !PlayerController.Instance.PlayerStateList.IsInvincible)
        {
            Attack();
            PlayerController.Instance.HitStopTime(0, 5, .2f);
        }
    }
}
