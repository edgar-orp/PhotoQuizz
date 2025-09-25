using UnityEngine;
using UnityEngine.InputSystem;

namespace dev.xrlab.photoquizz.Core
{
    public class HardResetHotkey : MonoBehaviour
    {
        [SerializeField] private AppController appController;

        private void Update()
        {
            // Detecta CTRL + R con el nuevo InputSystem
            if (Keyboard.current != null)
            {
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool r = Keyboard.current.rKey.wasPressedThisFrame;

                if (ctrl && r && appController != null)
                {
                    Debug.Log("[Hotkey] CTRL+R detectado → HardReset");
                    appController.HardRestartScene();
                }
            }
        }
    }
}
