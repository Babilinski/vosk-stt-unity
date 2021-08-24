using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class MLInputToggle : MonoBehaviour
{
    public Toggle Toggle;
    // Start is called before the first frame update
    void Start()
    {
        MLInput.OnControllerButtonDown += MLInputOnOnControllerButtonDown;
    }

    private void MLInputOnOnControllerButtonDown(byte controllerid, MLInput.Controller.Button button)
    {
        if (button == MLInput.Controller.Button.Bumper)
        {
            Toggle.isOn = !Toggle.isOn;
        }
    }

  
}
