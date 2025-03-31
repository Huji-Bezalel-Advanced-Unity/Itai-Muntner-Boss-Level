using UnityEngine;

namespace BBB.Scripts.Gameplay
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform groundCheck;

        private Rigidbody2D _rigidbody;
        private bool _isGrounded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            Move();
            Jump();
        }

        private void Move()
        {
            var moveInput = Input.GetAxis("Horizontal");
            var movement = new Vector2(moveInput * speed, _rigidbody.linearVelocity.y);
            _rigidbody.linearVelocity = movement;
        }

        private void Jump()
        {
            _isGrounded = Physics2D.OverlapCircle(groundCheck.position - groundCheck.localScale/2, 0.1f, groundLayer);

            if (_isGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || 
                                Input.GetKeyDown(KeyCode.W)))
            {
                _rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }
    }
}
