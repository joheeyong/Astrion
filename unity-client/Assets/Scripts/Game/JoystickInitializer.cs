using UnityEngine;

namespace Astrion.Game
{
    public class JoystickInitializer : MonoBehaviour
    {
        private void Start()
        {
            var joystick = GetComponent<Joystick>();
            if (joystick == null) return;

            var joystickArea = transform.Find("GameHUD_Canvas")?.Find("JoystickArea")
                ?? transform.Find("JoystickArea");
            if (joystickArea == null)
            {
                // Search children
                foreach (Transform child in transform)
                {
                    if (child.name == "JoystickArea")
                    {
                        joystickArea = child;
                        break;
                    }
                }
            }

            if (joystickArea == null) return;

            var bg = joystickArea.GetComponent<RectTransform>();
            var handle = joystickArea.Find("JoystickHandle")?.GetComponent<RectTransform>();
            if (bg != null && handle != null)
                joystick.Init(bg, handle);

            // Connect joystick to player
            var player = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
            if (player != null)
            {
                var controller = player.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    var field = typeof(SimplePlayerController).GetField("joystick",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                        field.SetValue(controller, joystick);
                }
            }
        }
    }
}
