using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] protected float health;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;

    [SerializeField] protected float speed;
    [SerializeField] protected float damage;

    protected float recoilTimer = 0f;
    protected new Rigidbody2D rigidbody2D;

    protected virtual void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (health <= 0)
        {
            Die();
        }

        if (isRecoiling)
        {
            if (recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0f;
            }
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    public virtual void EnemyHit(float _damage, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damage;
        if (!isRecoiling)
        {
            rigidbody2D.AddForce(-_hitForce * recoilFactor * _hitDirection);
            isRecoiling = true;
        }
    }

    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(damage);
    }

    protected void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("Player") && !PlayerController.Instance.playerStateList.isInvincible)
        {
            Attack();
        }
    }
}
