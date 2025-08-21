using UnityEngine;

public class EnemyZombie : EnemyController
{
    protected override void Start()
    {
        base.Start();
        m_rigidbody2D.gravityScale = 12f;
    }

    protected override void Update()
    {
        base.Update();
        if (!m_isRecoiling)
        {
            transform.position = Vector2
            .MoveTowards(
                transform.position,
                new Vector2(PlayerController.Instance.transform.position.x, transform.position.y),
                m_speed * Time.deltaTime
            );
        }
    }

    public override void EnemyHit(float _damage, Vector2 _hitDirection, float _hitForce)
    {
        base.EnemyHit(_damage, _hitDirection, _hitForce);
    }
}
