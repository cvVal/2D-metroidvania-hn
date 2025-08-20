using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private int health;
    [SerializeField] public int maxHealth;
    [SerializeField] private float hitFlashSpeed;
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;

    private float healTimer;
    [SerializeField] private float timeToHeal;
    [SerializeField] private GameObject bloodSpurtPrefab;

    [Space(5f)]

    // ========================================================== //

    [Header("Mana Settings")]
    [SerializeField] private float mana;
    [SerializeField] private float manaDrainSpeed;
    [SerializeField] private float manaGain;

    [Space(5f)]

    // ========================================================== //

    [Header("Player Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [Space(5f)]

    // ========================================================== //

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 45f; // Force applied when jumping (higher = higher jumps)

    // Jump Buffer: Allows player to press jump slightly before landing and still jump
    private float jumpBufferCounter = 0; // Current buffer timer countdown
    [SerializeField] private int jumpBufferFrames; // How many frames to remember jump input

    // Coyote Time: Allows player to jump for a short time after leaving ground
    private float coyoteTimeCounter = 0; // Current coyote time remaining
    [SerializeField] private float coyoteTime; // Duration player can jump after leaving ground

    // Air Jumping: Multiple jumps while airborne (double jump, triple jump, etc.)
    private int airJumpCounter = 0; // Current number of air jumps used
    [SerializeField] private int maxAirJumps; // Maximum air jumps allowed before landing
    [Space(5f)]

    // ========================================================== //

    [Header("Dash Settings")]
    [SerializeField] private GameObject dashEffect; // Visual effect spawned during dash
    [SerializeField] private float dashSpeed = 20f; // Speed during dash movement
    [SerializeField] private float dashTime = 0.2f; // Duration of dash in seconds
    [SerializeField] private float dashCooldown = 1f; // Cooldown before dash can be used again
    private bool canDash = true; // Whether dash is available (not on cooldown)
    private float gravity; // Stores original gravity to restore after dash
    private bool isDashing = false; // Current dash state flag
    [Space(5f)]

    // ========================================================== //

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [Space(5f)]

    // ========================================================== //

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f; // Damage dealt by attacks
    [SerializeField] private float timeBetweenAttacks; // Cooldown between attacks
    private float timeSinceAttack;
    private bool isAttacking = false; // Whether attack input was pressed this frame
    [SerializeField] Transform sideAttackPoint, upAttackPoint, downAttackPoint; // Attack points for different attack directions
    [SerializeField] Vector2 sideAttackArea, upAttackArea, downAttackArea; // Area of effect for each attack direction
    [SerializeField] private LayerMask attackableLayer; // Layer mask for attackable objects
    [SerializeField] private GameObject slashEffect; // Visual effect for attacks
    private bool restoreTime;
    private float restoreTimeSpeed;
    [Space(5f)]

    // ========================================================== //

    [Header("Recoil Settings")]
    [SerializeField] private int recoilXSteps = 5;
    [SerializeField] private int recoilYSteps = 5;
    [SerializeField] private float recoilXSpeed = 100;
    [SerializeField] private float recoilYSpeed = 100;
    private int stepsXRecoiled, stepsYRecoiled;
    [Space(5f)]

    // ========================================================== //

    private new Rigidbody2D rigidbody2D;
    private float xAxisInput, yAxisInput;

    private Animator animator;

    [HideInInspector] public PlayerStateList playerStateList;

    private SpriteRenderer spriteRenderer;

    // ========================================================== //

    public int Health
    {
        get { return health; }
        set
        {
            if (health != value)
            {
                health = Mathf.Clamp(value, 0, maxHealth);

                if (onHealthChangedCallback != null)
                {
                    onHealthChangedCallback.Invoke();
                }
            }
        }
    }

    float Mana
    {
        get { return mana; }
        set
        {
            if (mana != value)
            {
                mana = Mathf.Clamp(value, 0, 1);
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

        playerStateList = GetComponent<PlayerStateList>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        gravity = rigidbody2D.gravityScale;
        Health = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        Mana = mana;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpState();

        if (playerStateList.isDashing) return; // Skip movement and jumping if dashing
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
        if (playerStateList.isDashing) return; // Skip movement and jumping if dashing
        Recoil();
    }

    private void GetInputs()
    {
        xAxisInput = Input.GetAxis("Horizontal");
        yAxisInput = Input.GetAxis("Vertical");
        isAttacking = Input.GetButtonDown("Fire1"); // Uses Input Manager settings (left ctrl OR left mouse)
    }

    private void Move()
    {
        rigidbody2D.linearVelocity = new Vector2(xAxisInput * walkSpeed, rigidbody2D.linearVelocity.y);
        animator.SetBool("isWalking", rigidbody2D.linearVelocity.x != 0 && IsGrounded());
    }

    private void Jump()
    {
        if (Input.GetButtonUp("Jump") && rigidbody2D.linearVelocity.y > 0)
        {
            rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, 0);
            playerStateList.isJumping = false;
        }

        if (!playerStateList.isJumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, jumpForce);
                playerStateList.isJumping = true;
            }
            else if (!IsGrounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                playerStateList.isJumping = true;
                airJumpCounter++;
                rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, jumpForce);
            }
        }

        animator.SetBool("isJumping", !IsGrounded());
    }

    private bool IsGrounded()
    {
        if (Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckY, groundLayer)
            || Physics2D.Raycast(groundCheck.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, groundLayer)
            || Physics2D.Raycast(groundCheck.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, groundLayer))
        {
            return true;
        }
        return false;
    }

    private void Flip()
    {
        if (xAxisInput < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            playerStateList.isLookingRight = false;
        }
        else if (xAxisInput > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            playerStateList.isLookingRight = true;
        }
    }

    private void UpdateJumpState()
    {
        if (IsGrounded())
        {
            playerStateList.isJumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime * 10;
        }
    }

    private void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !isDashing)
        {
            StartCoroutine(Dash());
            isDashing = true;
        }

        if (IsGrounded())
        {
            isDashing = false;
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        playerStateList.isDashing = true;
        animator.SetTrigger("Dashing");

        rigidbody2D.gravityScale = 0;
        rigidbody2D.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);

        if (IsGrounded()) Instantiate(dashEffect, transform);

        yield return new WaitForSeconds(dashTime);
        rigidbody2D.gravityScale = gravity;
        playerStateList.isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void Attack()
    {
        // Update attack cooldown timer
        timeSinceAttack += Time.deltaTime;
        if (isAttacking && timeSinceAttack >= timeBetweenAttacks)
        {
            // Reset cooldown timer
            timeSinceAttack = 0f;
            animator.SetTrigger("Attacking");

            if (yAxisInput == 0 || yAxisInput < 0 && IsGrounded())
            {
                Hit(sideAttackPoint, sideAttackArea, ref playerStateList.isRecoilingX, recoilXSpeed);
                Instantiate(slashEffect, sideAttackPoint);
            }
            else if (yAxisInput > 0)
            {
                Hit(upAttackPoint, upAttackArea, ref playerStateList.isRecoilingY, recoilYSpeed);
                SlashEffectAngle(slashEffect, 80, upAttackPoint);
            }
            else if (yAxisInput < 0 && !IsGrounded())
            {
                Hit(downAttackPoint, downAttackArea, ref playerStateList.isRecoilingY, recoilYSpeed);
                SlashEffectAngle(slashEffect, -90, downAttackPoint);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sideAttackPoint.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackPoint.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackPoint.position, downAttackArea);
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilForce)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0f, attackableLayer);
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
                        attackDamage,
                        (transform.position - enemy.transform.position).normalized,
                        _recoilForce
                    );

                if (enemy.CompareTag("Enemy"))
                {
                    Mana += manaGain;
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
        if (playerStateList.isRecoilingX)
        {
            if (playerStateList.isLookingRight)
            {
                rigidbody2D.linearVelocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rigidbody2D.linearVelocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if (playerStateList.isRecoilingY)
        {
            if (yAxisInput < 0)
            {
                rigidbody2D.gravityScale = 0;
                rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, recoilYSpeed);
            }
            else
            {
                rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, -recoilYSpeed);
            }

            airJumpCounter = 0;
        }
        else
        {
            rigidbody2D.gravityScale = gravity;
        }

        // Stop recoil effects
        if (playerStateList.isRecoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else if (playerStateList.isRecoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
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
        stepsXRecoiled = 0;
        playerStateList.isRecoilingX = false;
    }

    private void StopRecoilY()
    {
        stepsYRecoiled = 0;
        playerStateList.isRecoilingY = false;
    }

    public void TakeDamage(float _damage)
    {
        Health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }

    private IEnumerator StopTakingDamage()
    {
        playerStateList.isInvincible = true;

        GameObject bloodSpurt = Instantiate(bloodSpurtPrefab, transform.position, Quaternion.identity);
        Destroy(bloodSpurt, 1.5f);

        animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(1f); // Duration of invincibility
        playerStateList.isInvincible = false;
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        restoreTimeSpeed = _restoreSpeed;
        Time.timeScale = _newTimeScale;

        if (_delay > 0)
        {
            StopCoroutine(ResetTimeScale(_delay));
            StartCoroutine(ResetTimeScale(_delay));
        }
        else
        {
            restoreTime = true;
        }
    }

    private IEnumerator ResetTimeScale(float _delay)
    {
        restoreTime = true;
        yield return new WaitForSeconds(_delay);
    }

    private void RestoreTimeScale()
    {
        if (restoreTime)
        {
            if (Time.timeScale < 1f)
            {
                Time.timeScale += restoreTimeSpeed * Time.deltaTime;
            }
            else
            {
                Time.timeScale = 1f;
                restoreTime = false;
            }
        }
    }

    private void FlashWhileInvincible()
    {
        spriteRenderer.material.color = playerStateList.isInvincible
        ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1f))
        : Color.white;
    }

    private void Heal()
    {
        if (Input.GetButton("Healing")
                && Health < maxHealth
                && Mana > 0
                && !playerStateList.isJumping
                && !playerStateList.isDashing)
        {
            playerStateList.isHealing = true;
            animator.SetBool("isHealing", true);

            healTimer += Time.deltaTime;

            if (healTimer >= timeToHeal)
            {
                Health++;
                healTimer = 0;
            }

            // Drain mana
            Mana -= Time.deltaTime * manaDrainSpeed;
        }
        else
        {
            playerStateList.isHealing = false;
            animator.SetBool("isHealing", false);
            healTimer = 0;
        }
    }
}
