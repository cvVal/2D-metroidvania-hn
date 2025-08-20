using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private int m_health;
    [SerializeField] public int MaxHealth;
    [SerializeField] private float m_hitFlashSpeed;
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate OnHealthChangedCallback;

    private float m_healTimer;
    [SerializeField] private float m_timeToHeal;
    [SerializeField] private GameObject m_bloodSpurtPrefab;

    [Space(5f)]

    // ========================================================== //

    [Header("Mana Settings")]
    [SerializeField] private float m_mana;
    [SerializeField] private float m_manaDrainSpeed;
    [SerializeField] private float m_manaGain;

    [Space(5f)]

    // ========================================================== //

    [Header("Player Movement Settings")]
    [SerializeField] private float m_walkSpeed = 5f;
    [Space(5f)]

    // ========================================================== //

    [Header("Jump Settings")]
    [SerializeField] private float m_jumpForce = 45f; // Force applied when jumping (higher = higher jumps)

    // Jump Buffer: Allows player to press jump slightly before landing and still jump
    private float m_jumpBufferCounter = 0; // Current buffer timer countdown
    [SerializeField] private int m_jumpBufferFrames; // How many frames to remember jump input

    // Coyote Time: Allows player to jump for a short time after leaving ground
    private float m_coyoteTimeCounter = 0; // Current coyote time remaining
    [SerializeField] private float m_coyoteTime; // Duration player can jump after leaving ground

    // Air Jumping: Multiple jumps while airborne (double jump, triple jump, etc.)
    private int m_airJumpCounter = 0; // Current number of air jumps used
    [SerializeField] private int m_maxAirJumps; // Maximum air jumps allowed before landing
    [Space(5f)]

    // ========================================================== //

    [Header("Dash Settings")]
    [SerializeField] private GameObject m_dashEffect; // Visual effect spawned during dash
    [SerializeField] private float m_dashSpeed = 20f; // Speed during dash movement
    [SerializeField] private float m_dashTime = 0.2f; // Duration of dash in seconds
    [SerializeField] private float m_dashCooldown = 1f; // Cooldown before dash can be used again
    private bool m_canDash = true; // Whether dash is available (not on cooldown)
    private float m_gravity; // Stores original gravity to restore after dash
    private bool m_isDashing = false; // Current dash state flag
    [Space(5f)]

    // ========================================================== //

    [Header("Ground Check Settings")]
    [SerializeField] private Transform m_groundCheck;
    [SerializeField] private float m_groundCheckX = 0.5f;
    [SerializeField] private float m_groundCheckY = 0.2f;
    [SerializeField] private LayerMask m_groundLayer;
    [Space(5f)]

    // ========================================================== //

    [Header("Attack Settings")]
    [SerializeField] private float m_attackDamage = 10f; // Damage dealt by attacks
    [SerializeField] private float m_timeBetweenAttacks; // Cooldown between attacks
    private float m_timeSinceAttack;
    private bool m_isAttacking = false; // Whether attack input was pressed this frame
    [SerializeField] private Transform m_sideAttackPoint;
    [SerializeField] private Transform m_upAttackPoint;
    [SerializeField] private Transform m_downAttackPoint;
    [SerializeField] private Vector2 m_sideAttackArea;
    [SerializeField] private Vector2 m_upAttackArea;
    [SerializeField] private Vector2 m_downAttackArea;
    [SerializeField] private LayerMask m_attackableLayer; // Layer mask for attackable objects
    [SerializeField] private GameObject m_slashEffect; // Visual effect for attacks
    private bool m_restoreTime;
    private float m_restoreTimeSpeed;
    [Space(5f)]

    // ========================================================== //

    [Header("Recoil Settings")]
    [SerializeField] private int m_recoilXSteps = 5;
    [SerializeField] private int m_recoilYSteps = 5;
    [SerializeField] private float m_recoilXSpeed = 100;
    [SerializeField] private float m_recoilYSpeed = 100;
    private int m_stepsXRecoiled, m_stepsYRecoiled;
    [Space(5f)]

    // ========================================================== //

    private Rigidbody2D m_rigidbody2D;
    private float m_xAxisInput, m_yAxisInput;

    private Animator m_animator;

    [HideInInspector] public PlayerStateList PlayerStateList;

    private SpriteRenderer m_spriteRenderer;

    // ========================================================== //

    public int Health
    {
        get { return m_health; }
        set
        {
            if (m_health != value)
            {
                m_health = Mathf.Clamp(value, 0, MaxHealth);

                OnHealthChangedCallback?.Invoke();
            }
        }
    }

    float Mana
    {
        get { return m_mana; }
        set
        {
            if (m_mana != value)
            {
                m_mana = Mathf.Clamp(value, 0, 1);
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        PlayerStateList = GetComponent<PlayerStateList>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        m_gravity = m_rigidbody2D.gravityScale;
        Health = MaxHealth;
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        Mana = m_mana;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpState();

        if (PlayerStateList.IsDashing) return; // Skip movement and jumping if dashing
        // if (playerStateList.isAttacking) return; // Skip movement and jumping if attacking

        Flip();
        Move();
        Jump();
        StartDash();
        Attack();
        RestoreTimeScale();
        FlashWhileInvincible();
        Heal();
    }

    void FixedUpdate()
    {
        if (PlayerStateList.IsDashing) return; // Skip movement and jumping if dashing
        Recoil();
    }

    private void GetInputs()
    {
        m_xAxisInput = Input.GetAxis("Horizontal");
        m_yAxisInput = Input.GetAxis("Vertical");
        m_isAttacking = Input.GetButtonDown("Fire1"); // Uses Input Manager settings (left ctrl OR left mouse)
    }

    private void Move()
    {
        m_rigidbody2D.linearVelocity = new Vector2(m_xAxisInput * m_walkSpeed, m_rigidbody2D.linearVelocity.y);
        m_animator.SetBool("isWalking", m_rigidbody2D.linearVelocity.x != 0 && IsGrounded());
    }

    private void Jump()
    {
        if (Input.GetButtonUp("Jump") && m_rigidbody2D.linearVelocity.y > 0)
        {
            m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, 0);
            PlayerStateList.IsJumping = false;
        }

        if (!PlayerStateList.IsJumping)
        {
            if (m_jumpBufferCounter > 0 && m_coyoteTimeCounter > 0)
            {
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, m_jumpForce);
                PlayerStateList.IsJumping = true;
            }
            else if (!IsGrounded() && m_airJumpCounter < m_maxAirJumps && Input.GetButtonDown("Jump"))
            {
                PlayerStateList.IsJumping = true;
                m_airJumpCounter++;
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, m_jumpForce);
            }
        }

        m_animator.SetBool("isJumping", !IsGrounded());
    }

    private bool IsGrounded()
    {
        if (Physics2D.Raycast(m_groundCheck.position, Vector2.down, m_groundCheckY, m_groundLayer)
            || Physics2D.Raycast(m_groundCheck.position + new Vector3(m_groundCheckX, 0, 0), Vector2.down, m_groundCheckY, m_groundLayer)
            || Physics2D.Raycast(m_groundCheck.position + new Vector3(-m_groundCheckX, 0, 0), Vector2.down, m_groundCheckY, m_groundLayer))
        {
            return true;
        }
        return false;
    }

    private void Flip()
    {
        if (m_xAxisInput < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            PlayerStateList.IsLookingRight = false;
        }
        else if (m_xAxisInput > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            PlayerStateList.IsLookingRight = true;
        }
    }

    private void UpdateJumpState()
    {
        if (IsGrounded())
        {
            PlayerStateList.IsJumping = false;
            m_coyoteTimeCounter = m_coyoteTime;
            m_airJumpCounter = 0;
        }
        else
        {
            m_coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            m_jumpBufferCounter = m_jumpBufferFrames;
        }
        else
        {
            m_jumpBufferCounter -= Time.deltaTime * 10;
        }
    }

    private void StartDash()
    {
        if (Input.GetButtonDown("Dash") && m_canDash && !m_isDashing)
        {
            StartCoroutine(Dash());
            m_isDashing = true;
        }

        if (IsGrounded())
        {
            m_isDashing = false;
        }
    }

    private IEnumerator Dash()
    {
        m_canDash = false;
        PlayerStateList.IsDashing = true;
        m_animator.SetTrigger("Dashing");

        m_rigidbody2D.gravityScale = 0;
        m_rigidbody2D.linearVelocity = new Vector2(transform.localScale.x * m_dashSpeed, 0);

        if (IsGrounded()) Instantiate(m_dashEffect, transform);

        yield return new WaitForSeconds(m_dashTime);
        m_rigidbody2D.gravityScale = m_gravity;
        PlayerStateList.IsDashing = false;
        yield return new WaitForSeconds(m_dashCooldown);
        m_canDash = true;
    }

    private void Attack()
    {
        // Update attack cooldown timer
        m_timeSinceAttack += Time.deltaTime;
        if (m_isAttacking && m_timeSinceAttack >= m_timeBetweenAttacks)
        {
            // Reset cooldown timer
            m_timeSinceAttack = 0f;
            m_animator.SetTrigger("Attacking");

            if (m_yAxisInput == 0 || m_yAxisInput < 0 && IsGrounded())
            {
                Hit(m_sideAttackPoint, m_sideAttackArea, ref PlayerStateList.IsRecoilingX, m_recoilXSpeed);
                Instantiate(m_slashEffect, m_sideAttackPoint);
            }
            else if (m_yAxisInput > 0)
            {
                Hit(m_upAttackPoint, m_upAttackArea, ref PlayerStateList.IsRecoilingY, m_recoilYSpeed);
                SlashEffectAngle(m_slashEffect, 80, m_upAttackPoint);
            }
            else if (m_yAxisInput < 0 && !IsGrounded())
            {
                Hit(m_downAttackPoint, m_downAttackArea, ref PlayerStateList.IsRecoilingY, m_recoilYSpeed);
                SlashEffectAngle(m_slashEffect, -90, m_downAttackPoint);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(m_sideAttackPoint.position, m_sideAttackArea);
        Gizmos.DrawWireCube(m_upAttackPoint.position, m_upAttackArea);
        Gizmos.DrawWireCube(m_downAttackPoint.position, m_downAttackArea);
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilForce)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0f, m_attackableLayer);
        if (hitEnemies.Length > 0)
        {
            Debug.Log("Hit!");
            _recoilDir = true;
        }
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.GetComponent<EnemyController>() != null)
            {
                enemy.GetComponent<EnemyController>()
                    .EnemyHit(
                        m_attackDamage,
                        (transform.position - enemy.transform.position).normalized,
                        _recoilForce
                    );

                if (enemy.CompareTag("Enemy"))
                {
                    Mana += m_manaGain;
                }
            }
        }
    }

    private void SlashEffectAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y);
    }

    private void Recoil()
    {
        if (PlayerStateList.IsRecoilingX)
        {
            if (PlayerStateList.IsLookingRight)
            {
                m_rigidbody2D.linearVelocity = new Vector2(-m_recoilXSpeed, 0);
            }
            else
            {
                m_rigidbody2D.linearVelocity = new Vector2(m_recoilXSpeed, 0);
            }
        }

        if (PlayerStateList.IsRecoilingY)
        {
            if (m_yAxisInput < 0)
            {
                m_rigidbody2D.gravityScale = 0;
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, m_recoilYSpeed);
            }
            else
            {
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, -m_recoilYSpeed);
            }

            m_airJumpCounter = 0;
        }
        else
        {
            m_rigidbody2D.gravityScale = m_gravity;
        }

        // Stop recoil effects
        if (PlayerStateList.IsRecoilingX && m_stepsXRecoiled < m_recoilXSteps)
        {
            m_stepsXRecoiled++;
        }
        else if (PlayerStateList.IsRecoilingY && m_stepsYRecoiled < m_recoilYSteps)
        {
            m_stepsYRecoiled++;
        }
        else
        {
            StopRecoilX();
            StopRecoilY();
        }

        if (IsGrounded())
        {
            StopRecoilY();
        }
    }

    private void StopRecoilX()
    {
        m_stepsXRecoiled = 0;
        PlayerStateList.IsRecoilingX = false;
    }

    private void StopRecoilY()
    {
        m_stepsYRecoiled = 0;
        PlayerStateList.IsRecoilingY = false;
    }

    public void TakeDamage(float _damage)
    {
        Health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }

    private IEnumerator StopTakingDamage()
    {
        PlayerStateList.IsInvincible = true;

        GameObject bloodSpurt = Instantiate(m_bloodSpurtPrefab, transform.position, Quaternion.identity);
        Destroy(bloodSpurt, 1.5f);

        m_animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(1f); // Duration of invincibility
        PlayerStateList.IsInvincible = false;
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        m_restoreTimeSpeed = _restoreSpeed;
        Time.timeScale = _newTimeScale;

        if (_delay > 0)
        {
            StopCoroutine(ResetTimeScale(_delay));
            StartCoroutine(ResetTimeScale(_delay));
        }
        else
        {
            m_restoreTime = true;
        }
    }

    private IEnumerator ResetTimeScale(float _delay)
    {
        m_restoreTime = true;
        yield return new WaitForSeconds(_delay);
    }

    private void RestoreTimeScale()
    {
        if (m_restoreTime)
        {
            if (Time.timeScale < 1f)
            {
                Time.timeScale += m_restoreTimeSpeed * Time.deltaTime;
            }
            else
            {
                Time.timeScale = 1f;
                m_restoreTime = false;
            }
        }
    }

    private void FlashWhileInvincible()
    {
        m_spriteRenderer.material.color = PlayerStateList.IsInvincible
        ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * m_hitFlashSpeed, 1f))
        : Color.white;
    }

    private void Heal()
    {
        if (Input.GetButton("Healing")
                && Health < MaxHealth
                && Mana > 0
                && !PlayerStateList.IsJumping
                && !PlayerStateList.IsDashing)
        {
            PlayerStateList.IsHealing = true;
            m_animator.SetBool("isHealing", true);

            m_healTimer += Time.deltaTime;

            if (m_healTimer >= m_timeToHeal)
            {
                Health++;
                m_healTimer = 0;
            }

            // Drain mana
            Mana -= Time.deltaTime * m_manaDrainSpeed;
        }
        else
        {
            PlayerStateList.IsHealing = false;
            m_animator.SetBool("isHealing", false);
            m_healTimer = 0;
        }
    }
}
