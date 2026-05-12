using UnityEngine;

namespace Astrion.Game
{
    public class JoystickInitializer : MonoBehaviour
    {
        private void Start()
        {
            var joystick = GetComponent<Joystick>();
            if (joystick == null) return;

            var bg = GetComponent<RectTransform>();
            var handle = transform.Find("JoystickHandle")?.GetComponent<RectTransform>();
            if (bg != null && handle != null)
                joystick.Init(bg, handle);

            var player = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController2D>();
                if (controller != null) controller.SetJoystick(joystick);
            }
        }
    }
}
