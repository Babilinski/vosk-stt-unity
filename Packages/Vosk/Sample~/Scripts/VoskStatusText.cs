using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoskStatusText : MonoBehaviour
{

    public VoskSpeechToText VoskSpeechToText;
    public Text StatusText;

    void Awake()
    {
        VoskSpeechToText.OnStatusUpdated += OnStatusUpdated;
    }

    private void OnStatusUpdated(string obj)
    {
        Debug.Log(obj);
        StatusText.text = obj;
    }
}
