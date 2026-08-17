using UnityEngine;
using UnityEngine.InputSystem;

namespace BossLevel.Player
{
    /// <summary>
    /// The one place player input is read. Everything else asks this component what the player
    /// intends rather than touching the Input System itself.
    /// </summary>
    /// <remarks>
    /// Actions are looked up on the <see cref="InputActionAsset"/> by name instead of through a
    /// generated wrapper class. That keeps every action name in this single file, and it means
    /// the project compiles without anyone having to tick "Generate C# Class" on the asset first.
    /// <para>
    /// Funnelling input through one component is also what lets the game take control away
    /// during the intro and the end of the fight: disabling this object is enough, and no other
    /// script needs to know those states exist.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        private const string PlayerMapName = "Player";
        private const string MoveActionName = "Move";
        private const string JumpActionName = "Jump";
        private const string FireActionName = "Attack";

        /// <summary>
        /// Dash reuses the template's Sprint action, which is already bound to Left Shift.
        /// </summary>
        private const string DashActionName = "Sprint";

        [Tooltip("The project's InputSystem_Actions asset.")]
        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap _playerMap;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
        private InputAction _fire;

        /// <summary>Horizontal movement intent, from -1 (left) to 1 (right).</summary>
        public float HorizontalMove => _move?.ReadValue<Vector2>().x ?? 0f;

        /// <summary>True only on the frame the jump button went down. Drives the jump buffer.</summary>
        public bool JumpPressed => _jump?.WasPressedThisFrame() ?? false;

        /// <summary>True while jump is held down. Drives variable jump height.</summary>
        public bool JumpHeld => _jump?.IsPressed() ?? false;

        /// <summary>True only on the frame the dash button went down.</summary>
        public bool DashPressed => _dash?.WasPressedThisFrame() ?? false;

        /// <summary>True while the fire button is held. Shooting is rate-limited, not per-press.</summary>
        public bool FireHeld => _fire?.IsPressed() ?? false;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} has no input actions assigned.", this);
                enabled = false;
                return;
            }

            _playerMap = inputActions.FindActionMap(PlayerMapName, throwIfNotFound: true);

            _move = _playerMap.FindAction(MoveActionName, throwIfNotFound: true);
            _jump = _playerMap.FindAction(JumpActionName, throwIfNotFound: true);
            _dash = _playerMap.FindAction(DashActionName, throwIfNotFound: true);
            _fire = _playerMap.FindAction(FireActionName, throwIfNotFound: true);
        }

        private void OnEnable()
        {
            _playerMap?.Enable();
        }

        private void OnDisable()
        {
            _playerMap?.Disable();
        }
    }
}
