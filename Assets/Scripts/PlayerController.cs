using System;
using NUnit.Framework;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Player Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;

    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 45f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private new Rigidbody2D rigidbody2D;
    private float xAxisInput;

    private Animator animator;

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
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        Move();
        Jump();
        Flip();
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
        }

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, jumpForce);
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
}
