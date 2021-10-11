using UnityEngine;
using UnityEngine.UI;
public class VoskResultText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;
    public Text ResultText;

    void Awake()
    {
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
    }

    private void OnTranscriptionResult(string obj)
    {
        Debug.Log(obj);
        ResultText.text = "Recognized: ";
       var result = new RecognitionResult(obj);
        for (int i = 0; i < result.Phrases.Length; i++)
        {
            if (i > 0)
            {
                ResultText.text += "\n ---------- \n";
            }

            var confidence = result.Phrases[0].Confidence != 0 ? " | Confidence: " + Sigmoid(result.Phrases[0].Confidence) : "";
            ResultText.text += result.Phrases[0].Text + confidence;
        }
    }
    public static float Sigmoid(float x)
    {
        return (1.0f / (1.0f + Mathf.Exp(-x)));
    }
}
