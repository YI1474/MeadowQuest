using UnityEngine;
using UnityEngine.InputSystem;

namespace MeadowQuest
{
    // Reads input only. Does not move the player or change animation.
    public sealed class KeyboardMoveInput : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset controls;
        InputActionAsset runtimeControls;
        InputAction move;
        InputAction interact;
        public bool InteractPressed => interact != null && interact.WasPressedThisFrame();
        public Vector2Int Direction { get; private set; }
        public bool ActionEnabled => move != null && move.enabled;

        void Initialize()
        {
            if (move != null)
                return;
            if (!controls)
                controls = Resources.Load<InputActionAsset>("Input/PlayerControls");
            if (!controls)
            {
                Debug.LogError("PlayerControls input actions asset is missing.", this);
                enabled = false;
                return;
            }

            // Each player owns its enabled state; never mutate the shared asset.
            runtimeControls = Instantiate(controls);
            move = runtimeControls.FindAction("Gameplay/Move", true);
            interact = runtimeControls.FindAction("Gameplay/Interact", true);
        }

        void OnEnable()
        {
            Initialize();
            runtimeControls?.Enable();
        }

        void Update()
        {
            if (!Application.isFocused || move == null)
            {
                Direction = Vector2Int.zero;
                return;
            }

            Vector2 value = move.ReadValue<Vector2>();
            int x = Mathf.Abs(value.x) > .5f ? (int)Mathf.Sign(value.x) : 0;
            int y = Mathf.Abs(value.y) > .5f ? (int)Mathf.Sign(value.y) : 0;
            Direction = x != 0 ? new Vector2Int(x, 0) : new Vector2Int(0, y);
        }

        void OnDisable()
        {
            runtimeControls?.Disable();
            Direction = Vector2Int.zero;
        }

        void OnDestroy()
        {
            if (runtimeControls)
                Destroy(runtimeControls);
        }
    }
}
