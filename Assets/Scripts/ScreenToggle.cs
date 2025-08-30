using UnityEngine;

namespace dev.xrlab
{
    public class ScreenToggle : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.R))
            {
                ToggleFullscreen();
            }
        }

        private void ToggleFullscreen()
        {
            if (Screen.fullScreen)
            {
                Screen.SetResolution(1080, 1920, false);
            }
            else
            {
                Screen.SetResolution(1080, 1920, true);
            }
        }
    }
}