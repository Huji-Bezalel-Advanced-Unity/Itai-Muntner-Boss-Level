using System.Collections;
using BBB.Scripts.Gameplay;
using UnityEngine;

namespace BBB.Scripts.Core
{
    public class PassThroughPlatform : MonoBehaviour
    {
        private Collider2D _collider;
        private bool _playerOnPlatform;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!_playerOnPlatform ||
                (!Input.GetKeyDown(KeyCode.DownArrow) && !Input.GetKeyDown(KeyCode.S))) return;
            _collider.enabled = false;
            StartCoroutine(EnableCollider());
        }

        private IEnumerator EnableCollider()
        {
            yield return new WaitForSeconds(0.5f);
            _collider.enabled = true;
        }

        private void SetPlayerOnPlatform(Collision2D other, bool value)
        {
            var player = other.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                _playerOnPlatform = value;
            }
        }
        
        private void OnCollisionEnter2D(Collision2D other)
        {
            SetPlayerOnPlatform(other, true);
        }
        
        private void OnCollisionExit2D(Collision2D other)
        {
            SetPlayerOnPlatform(other, false);
        }
    }
}
