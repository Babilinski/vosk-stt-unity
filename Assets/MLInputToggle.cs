using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if PLATFORM_LUMIN
using UnityEngine.XR.MagicLeap;
#endif
public class MLInputToggle : MonoBehaviour
{
    public Toggle Toggle;
    // Start is called before the first frame update
    void Start()
    {
#if PLATFORM_LUMIN
        MLInput.OnControllerButtonDown += MLInputOnOnControllerButtonDown;
        MLInput.OnControllerButtonUp += MLInputOnOnControllerButtonDown;
#endif
    }


#if PLATFORM_LUMIN
    private void MLInputOnOnControllerButtonDown(byte controllerid, MLInput.Controller.Button button)
    {
        if (button == MLInput.Controller.Button.Bumper)
        {
            Toggle.isOn = !Toggle.isOn;
        }
    }
#endif

  
}
