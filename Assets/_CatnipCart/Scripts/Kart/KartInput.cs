using UnityEngine;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Input interface that both player input and AI input implement.
    /// This decouples the kart controller from the input source.
    /// </summary>
    public interface IKartInput
    {
        /// <summary>Acceleration input: 0 to 1</summary>
        float Accelerate { get; }

        /// <summary>Brake/reverse input: 0 to 1</summary>
        float Brake { get; }

        /// <summary>Steering input: -1 (left) to 1 (right)</summary>
        float Steer { get; }

        /// <summary>Whether the drift button is held</summary>
        bool Drift { get; }

        /// <summary>Whether the use item button was pressed this frame</summary>
        bool UseItem { get; }

        /// <summary>Whether looking back</summary>
        bool LookBack { get; }
    }

    /// <summary>
    /// Player input reader using Unity's legacy Input system.
    /// Supports WASD, Arrow Keys, and Gamepad.
    /// Uses only standard Unity input axes to avoid missing axis errors.
    /// </summary>
    public class KartInput : MonoBehaviour, IKartInput
    {
        public float Accelerate
        {
            get
            {
                float kb = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f;
                // Vertical axis positive = up/W (gamepad left stick up)
                float gp = Mathf.Clamp01(Input.GetAxis("Vertical"));
                return Mathf.Clamp01(kb + gp);
            }
        }

        public float Brake
        {
            get
            {
                float kb = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0f;
                float gp = Mathf.Clamp01(-Input.GetAxis("Vertical"));
                return Mathf.Clamp01(kb + gp);
            }
        }

        public float Steer
        {
            get
            {
                float keyboard = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboard -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboard += 1f;

                float gamepad = Input.GetAxis("Horizontal");

                // Use whichever has larger magnitude
                return Mathf.Abs(keyboard) > Mathf.Abs(gamepad) ? keyboard : gamepad;
            }
        }

        public bool Drift => Input.GetKey(KeyCode.Space) ||
                             Input.GetKey(KeyCode.LeftShift);

        public bool UseItem => Input.GetKeyDown(KeyCode.E) ||
                               Input.GetKeyDown(KeyCode.Return);

        public bool LookBack => Input.GetKey(KeyCode.Q) ||
                                Input.GetKey(KeyCode.Tab);
    }
}
