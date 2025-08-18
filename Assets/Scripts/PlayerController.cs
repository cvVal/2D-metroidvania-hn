using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

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

    private new Rigidbody2D rigidbody2D;
    private float xAxisInput;

    private Animator animator;

    private PlayerStateList playerStateList;

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
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerStateList = GetComponent<PlayerStateList>();
        gravity = rigidbody2D.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpState();

        if (playerStateList.isDashing) return; // Skip movement and jumping if dashing

        Flip();
        Move();
        Jump();
        StartDash();
    }

    private void GetInputs()
    {
        xAxisInput = Input.GetAxis("Horizontal");
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
        }
        else if (xAxisInput > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
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
}
