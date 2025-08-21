using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private int m_health;
    [SerializeField] private int m_maxHealth; // encapsulated backing field
    [SerializeField][Min(0f)] private float m_hitFlashSpeed;
    public delegate void OnHealthChangedDelegate();
    public event OnHealthChangedDelegate OnHealthChangedCallback; // safer than exposing delegate field

    private float m_healTimer;
    [SerializeField] private float m_timeToHeal;
    [SerializeField] private GameObject m_bloodSpurtPrefab;

    [Space(5f)]

    // ========================================================== //

    [Header("Mana Settings")]
    [SerializeField][Range(0f, 1f)] private float m_mana;
    [SerializeField][Min(0f)] private float m_manaDrainSpeed;
    [SerializeField][Range(0f, 1f)] private float m_manaGain;
    [SerializeField] private Image m_manaStorage;

    [Space(5f)]

    // ========================================================== //

    [Header("Player Movement Settings")]
    [SerializeField][Min(0f)] private float m_walkSpeed = 5f;
    [Space(5f)]

    // ========================================================== //

    [Header("Jump Settings")]
    [SerializeField][Min(0f)] private float m_jumpForce = 45f; // Force applied when jumping (higher = higher jumps)

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
    [SerializeField][Min(0f)] private float m_dashSpeed = 20f; // Speed during dash movement
    [SerializeField][Min(0f)] private float m_dashTime = 0.2f; // Duration of dash in seconds
    [SerializeField][Min(0f)] private float m_dashCooldown = 1f; // Cooldown before dash can be used again
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
    [SerializeField][Min(0f)] private float m_attackDamage = 10f; // Damage dealt by attacks
    [SerializeField][Min(0f)] private float m_timeBetweenAttacks; // Cooldown between attacks
    private float m_timeSinceAttack;
    private bool m_isAttacking = false; // Whether attack input was pressed this frame
    [SerializeField] private Transform m_sideAttackPoint;
    [SerializeField] private Vector2 m_sideAttackArea;
    [SerializeField] private Transform m_upAttackPoint;
    [SerializeField] private Vector2 m_upAttackArea;
    [SerializeField] private Transform m_downAttackPoint;
    [SerializeField] private Vector2 m_downAttackArea;
    [SerializeField] private LayerMask m_attackableLayer; // Layer mask for attackable objects
    [SerializeField] private GameObject m_slashEffect; // Visual effect for attacks
    // COMMENTED OUT: Time manipulation variables to remove slow-motion effect
    // private bool m_restoreTime;
    // private float m_restoreTimeSpeed;
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

    [Header("Spell Settings")]
    [SerializeField][Range(0f, 1f)] private float m_manaSpellCost = 0.3f;
    [SerializeField][Min(0f)] private float m_timeBetweenCast = 0.5f;
    [SerializeField] private float m_spellDamage;
    [SerializeField] private float m_downSpellForce;
    [SerializeField] private GameObject m_sideSpellFireball;
    [SerializeField] private GameObject m_downSpellFireball;
    [SerializeField] private GameObject m_upSpellExplosion;
    private float m_timeSinceCast;
    private float m_castOrHealTimer;
    [Space(5f)]

    [Header("Timing Settings")]
    [SerializeField][Min(0f)] private float m_invincibilityDuration = 1f;
    [SerializeField][Min(0f)] private float m_spellCastWindup = 0.15f;
    [SerializeField][Min(0f)] private float m_spellCastRecovery = 0.35f;

    // ========================================================== //

    private Rigidbody2D m_rigidbody2D;
    private float m_xAxisInput, m_yAxisInput;
    private bool m_jumpPressed, m_jumpReleased, m_dashPressed, m_attackPressed, m_healHeld, m_castReleased, m_castOrHealPressed;
    private bool m_isGroundedCached; // ground state cached per Update

    // Physics input buffering for FixedUpdate
    private float m_moveInputBuffered;
    private bool m_jumpRequestBuffered;
    private bool m_jumpCutRequestBuffered;
    private bool m_downSpellForceActive;

    private Animator m_animator;

    [HideInInspector] public PlayerStateList PlayerStateList;

    private SpriteRenderer m_spriteRenderer;

    // Animator parameter hashes (cached to avoid repeated string lookups)
    static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    static readonly int DashingHash = Animator.StringToHash("Dashing");
    static readonly int AttackingHash = Animator.StringToHash("Attacking");
    static readonly int HurtHash = Animator.StringToHash("Hurt");
    static readonly int IsHealingHash = Animator.StringToHash("IsHealing");
    static readonly int IsCastingHash = Animator.StringToHash("IsCasting");

    // Coroutine handle for controlled time scale reset
    private Coroutine m_timeScaleCoroutine;

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

    public int MaxHealth => m_maxHealth;
    public bool IsDownSpellActive => m_downSpellFireball != null && m_downSpellFireball.activeInHierarchy; // helper for external damage checks

    public event System.Action<float> OnManaChanged; // passes new mana value

    float Mana
    {
        get { return m_mana; }
        set
        {
            if (m_mana != value)
            {
                m_mana = Mathf.Clamp(value, 0, 1);
                m_manaStorage.fillAmount = Mana;
                OnManaChanged?.Invoke(m_mana);
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
        DontDestroyOnLoad(gameObject);
    }

    void OnValidate()
    {
        if (m_maxHealth < 1) m_maxHealth = 1;
        if (m_health > m_maxHealth) m_health = m_maxHealth;
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
        m_manaStorage.fillAmount = Mana;
        
        m_jumpBufferCounter = 0;
        m_coyoteTimeCounter = 0;
        m_airJumpCounter = 0;
        m_jumpRequestBuffered = false;
        m_jumpCutRequestBuffered = false;
        PlayerStateList.IsJumping = false;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        m_isGroundedCached = IsGrounded(); // cache once per frame
        UpdateJumpState();

        // Buffer physics inputs for FixedUpdate
        BufferPhysicsInputs();

        if (PlayerStateList.IsDashing) return; // still allow recoil in FixedUpdate only

        RestoreTimeScale();
        FlashWhileInvincible();
        Heal();
        CastSpell();

        if (PlayerStateList.IsHealing) return;

        Flip();
        StartDash();
        Attack();
    }

    void FixedUpdate()
    {
        if (PlayerStateList.IsDashing)
        {
            Recoil(); // still allow recoil during dash
            return;
        }

        // Apply all physics operations in FixedUpdate
        ApplyMovement();
        ApplyJump();
        ApplyDownSpellForce();
        Recoil();
    }

    private void GetInputs()
    {
        m_xAxisInput = Input.GetAxis("Horizontal");
        m_yAxisInput = Input.GetAxis("Vertical");
        m_jumpPressed = Input.GetButtonDown("Jump");
        m_jumpReleased = Input.GetButtonUp("Jump");
        m_dashPressed = Input.GetButtonDown("Dash");
        m_attackPressed = Input.GetButtonDown("Attack");
        m_healHeld = Input.GetButton("Cast/Heal");
        m_castReleased = Input.GetButtonUp("Cast/Heal");
        m_isAttacking = m_attackPressed;
        m_castOrHealPressed = m_healHeld;
        if (m_castOrHealPressed)
        {
            m_castOrHealTimer += Time.deltaTime;
        }
        else
        {
            m_castOrHealTimer = 0;
        }
    }

    private void BufferPhysicsInputs()
    {
        // Buffer movement input for FixedUpdate
        m_moveInputBuffered = m_xAxisInput;
        
        // Buffer jump requests
        if (m_jumpPressed)
            m_jumpRequestBuffered = true;
            
        if (m_jumpReleased && m_rigidbody2D.linearVelocity.y > 0)
            m_jumpCutRequestBuffered = true;
    }

    private void ApplyMovement()
    {
        if (PlayerStateList.IsHealing) return;

        m_rigidbody2D.linearVelocity = new Vector2(m_moveInputBuffered * m_walkSpeed, m_rigidbody2D.linearVelocity.y);
        m_animator.SetBool(IsWalkingHash, m_rigidbody2D.linearVelocity.x != 0 && m_isGroundedCached);
    }

    private void ApplyJump()
    {
        // Handle jump cut first
        if (m_jumpCutRequestBuffered && m_rigidbody2D.linearVelocity.y > 3)
        {
            m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, 0);
            PlayerStateList.IsJumping = false;
            m_jumpCutRequestBuffered = false;
        }

        // Handle jump initiation - only if there's a jump request
        if (m_jumpRequestBuffered)
        {
            // Ground jump (including coyote time)
            if (m_jumpBufferCounter > 0 && m_coyoteTimeCounter > 0 && !PlayerStateList.IsJumping)
            {
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, m_jumpForce);
                PlayerStateList.IsJumping = true;
                m_coyoteTimeCounter = 0; // Clear coyote time after jump
            }
            // Air jump (double jump, triple jump, etc.)
            else if (!m_isGroundedCached && m_airJumpCounter < m_maxAirJumps)
            {
                m_airJumpCounter++;
                m_rigidbody2D.linearVelocity = new Vector2(m_rigidbody2D.linearVelocity.x, m_jumpForce);
                PlayerStateList.IsJumping = true; // Ensure we're in jumping state
            }
            
            // Clear jump request after attempting jump
            m_jumpRequestBuffered = false;
        }

        m_animator.SetBool(IsJumpingHash, !m_isGroundedCached);
    }

    private void ApplyDownSpellForce()
    {
        if (m_downSpellForceActive && m_downSpellFireball.activeInHierarchy)
        {
            m_rigidbody2D.linearVelocity += m_downSpellForce * Vector2.down;
        }
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
        if (m_isGroundedCached)
        {
            PlayerStateList.IsJumping = false;
            m_coyoteTimeCounter = m_coyoteTime;
            m_airJumpCounter = 0;
        }
        else
        {
            m_coyoteTimeCounter -= Time.deltaTime;
        }

        // Update jump buffer - only when jump is actually pressed
        if (m_jumpPressed)
        {
            m_jumpBufferCounter = m_jumpBufferFrames;
        }
        else if (m_jumpBufferCounter > 0)
        {
            m_jumpBufferCounter -= Time.deltaTime * 60; // Convert frames to time-based countdown
        }
        
        // Clamp jump buffer to prevent negative values
        m_jumpBufferCounter = Mathf.Max(0, m_jumpBufferCounter);
    }

    private void StartDash()
    {
        if (m_dashPressed && m_canDash && !m_isDashing)
        {
            StartCoroutine(Dash());
            m_isDashing = true;
        }
        if (m_isGroundedCached)
        {
            m_isDashing = false;
        }
    }

    private IEnumerator Dash()
    {
        m_canDash = false;
        PlayerStateList.IsDashing = true;
        m_animator.SetTrigger(DashingHash);

        m_rigidbody2D.gravityScale = 0;
        int direction = PlayerStateList.IsLookingRight ? 1 : -1;
        m_rigidbody2D.linearVelocity = new Vector2(direction * m_dashSpeed, 0);
        if (IsGrounded() && m_dashEffect != null) Instantiate(m_dashEffect, transform);

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
            m_animator.SetTrigger(AttackingHash);

            if (m_yAxisInput == 0 || m_yAxisInput < 0 && m_isGroundedCached)
            {
                Hit(m_sideAttackPoint, m_sideAttackArea, ref PlayerStateList.IsRecoilingX, m_recoilXSpeed);
                if (m_slashEffect != null) Instantiate(m_slashEffect, m_sideAttackPoint);
            }
            else if (m_yAxisInput > 0)
            {
                Hit(m_upAttackPoint, m_upAttackArea, ref PlayerStateList.IsRecoilingY, m_recoilYSpeed);
                if (m_slashEffect != null) SlashEffectAngle(m_slashEffect, 80, m_upAttackPoint);
            }
            else if (m_yAxisInput < 0 && !m_isGroundedCached)
            {
                Hit(m_downAttackPoint, m_downAttackArea, ref PlayerStateList.IsRecoilingY, m_recoilYSpeed);
                if (m_slashEffect != null) SlashEffectAngle(m_slashEffect, -90, m_downAttackPoint);
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
        // Early return if no attack transform provided
        if (_attackTransform == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0f, m_attackableLayer);

        // Early return if no enemies hit
        if (hitEnemies.Length == 0) return;

        _recoilDir = true;

        // Process each hit enemy
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            ProcessEnemyHit(enemyCollider, _recoilForce);
        }
    }

    private void ProcessEnemyHit(Collider2D _enemyCollider, float _recoilForce)
    {
        EnemyController enemyController = _enemyCollider.GetComponent<EnemyController>();

        if (enemyController == null) return;

        // Calculate attack direction from player to enemy
        Vector2 attackDirection = (_enemyCollider.transform.position - transform.position).normalized;

        // Apply damage to enemy
        enemyController.EnemyHit(m_attackDamage, attackDirection, _recoilForce);

        // Grant mana if this is a tagged enemy
        if (_enemyCollider.CompareTag("Enemy"))
        {
            Mana += m_manaGain;
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
        if (PlayerStateList.IsRecoilingX) ApplyHorizontalRecoil();
        if (PlayerStateList.IsRecoilingY) ApplyVerticalRecoil(); else m_rigidbody2D.gravityScale = m_gravity;

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

        if (m_isGroundedCached)
        {
            StopRecoilY();
        }
    }

    private void ApplyHorizontalRecoil()
    {
        float dir = PlayerStateList.IsLookingRight ? -1f : 1f; // knock opposite of facing
        m_rigidbody2D.linearVelocity = new Vector2(dir * m_recoilXSpeed, 0);
    }

    private void ApplyVerticalRecoil()
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

        if (m_bloodSpurtPrefab != null)
        {
            GameObject bloodSpurt = Instantiate(m_bloodSpurtPrefab, transform.position, Quaternion.identity);
            Destroy(bloodSpurt, 1.5f);
        }
        m_animator.SetTrigger(HurtHash);
        yield return new WaitForSeconds(m_invincibilityDuration); // Duration of invincibility (serialized)
        PlayerStateList.IsInvincible = false;
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        // COMMENTED OUT: Time scale manipulation to remove slow-motion effect
        // _newTimeScale = Mathf.Max(_newTimeScale, 0.01f);

        // m_restoreTimeSpeed = _restoreSpeed;
        // Time.timeScale = _newTimeScale;

        // if (_delay > 0)
        // {
        //     if (m_timeScaleCoroutine != null) StopCoroutine(m_timeScaleCoroutine);
        //     m_timeScaleCoroutine = StartCoroutine(ResetTimeScale(_delay));
        // }
        // else
        // {
        //     m_restoreTime = true;
        // }

        // Visual feedback and invincibility are handled elsewhere (FlashWhileInvincible, StopTakingDamage)
    }

    private IEnumerator ResetTimeScale(float _delay)
    {
        // COMMENTED OUT: Time scale reset coroutine to remove slow-motion effect
        // yield return new WaitForSecondsRealtime(_delay);
        // m_restoreTime = true;
        yield break;
    }

    private void RestoreTimeScale()
    {
        // COMMENTED OUT: Time scale restoration to remove slow-motion effect
        // if (m_restoreTime)
        // {
        //     if (Time.timeScale < 1f)
        //     {
        //         Time.timeScale += m_restoreTimeSpeed * Time.unscaledDeltaTime;
        //     }
        //     else
        //     {
        //         Time.timeScale = 1f;
        //         m_restoreTime = false;
        //     }
        // }
    }

    private void FlashWhileInvincible()
    {
        m_spriteRenderer.material.color = PlayerStateList.IsInvincible
        ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * m_hitFlashSpeed, 1f))
        : Color.white;
    }

    private void Heal()
    {
        if (m_healHeld
            && m_castOrHealTimer > 0.05f
            && Health < MaxHealth
            && Mana > 0
            && m_isGroundedCached
            && !PlayerStateList.IsDashing)
        {
            PlayerStateList.IsHealing = true;
            m_animator.SetBool(IsHealingHash, true);

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
            m_animator.SetBool(IsHealingHash, false);
            m_healTimer = 0;
        }
    }

    private void CastSpell()
    {
        if (m_castReleased
                && m_castOrHealTimer <= 0.05f
                && m_timeSinceCast >= m_timeBetweenCast
                && Mana >= m_manaSpellCost)
        {
            PlayerStateList.IsCasting = true;
            m_timeSinceCast = 0;
            StartCoroutine(CastSpellCoroutine());
        }
        else
        {
            m_timeSinceCast += Time.deltaTime;
        }

        if (m_isGroundedCached)
        {
            m_downSpellFireball.SetActive(false); // Disable downspell if on the ground
            m_downSpellForceActive = false;
        }

        // Set flag for FixedUpdate to apply force
        m_downSpellForceActive = m_downSpellFireball.activeInHierarchy;
    }

    private IEnumerator CastSpellCoroutine()
    {
        m_animator.SetBool(IsCastingHash, true);
        yield return new WaitForSeconds(m_spellCastWindup);

        // side cast
        if (m_yAxisInput == 0 || (m_yAxisInput < 0 && m_isGroundedCached))
        {
            if (m_sideSpellFireball != null)
            {
                GameObject fireball = Instantiate(m_sideSpellFireball, m_sideAttackPoint.position, Quaternion.identity);

                // flip fireball
                if (PlayerStateList.IsLookingRight)
                {
                    fireball.transform.eulerAngles = Vector3.zero;
                }
                else
                {
                    fireball.transform.eulerAngles = new Vector2(fireball.transform.eulerAngles.x, 180f);
                }
            }

            PlayerStateList.IsRecoilingX = true;
        }
        // up cast
        else if (m_yAxisInput > 0)
        {
            if (m_upSpellExplosion != null) Instantiate(m_upSpellExplosion, transform);
            m_rigidbody2D.linearVelocity = Vector2.zero;
        }
        // down cast
        else if (m_yAxisInput < 0 && !m_isGroundedCached)
        {
            if (m_downSpellFireball != null) m_downSpellFireball.SetActive(true);
        }

        Mana -= m_manaSpellCost;
        yield return new WaitForSeconds(m_spellCastRecovery);
        m_animator.SetBool(IsCastingHash, false);
        PlayerStateList.IsCasting = false;
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (!PlayerStateList.IsCasting) return;
        EnemyController enemyController = _other.GetComponent<EnemyController>();
        if (enemyController == null) return;
        enemyController.EnemyHit(
            m_spellDamage,
            (_other.transform.position - transform.position).normalized,
            -m_recoilYSpeed
        );
    }
}
