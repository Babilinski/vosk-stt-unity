using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Vosk;

public class VoskSpeechToText : MonoBehaviour
{
    [Tooltip("Location of the model, relative to the Streaming Assets folder.")]
    public string ModelPath = "";

    [Tooltip("The source of the microphone input.")]
    public VoiceProcessor VoiceProcessor;

    [Tooltip("The phrases that will be detected. If left empty, all words will be detected.")]
    public List<string> KeyPhrases = new List<string>();

    [Tooltip("The Max number of alternatives that will be processed.")]
    public int MaxAlternatives = 3;

    [Tooltip("Should the recognizer start when the application is launched?")]
    public bool AutoStart = true;

    //Cached version of the Vosk Model.
    private Model _model;

    //Cached version of the Vosk recognizer.
    private VoskRecognizer _recognizer;

    //Conditional flag to see if a recognizer has already been created.
    //TODO: Allow for runtime changes to the recognizer.
    private bool _recognizerReady;

    //Holds all of the audio data until the user stops talking.
    private readonly List<short> _buffer = new List<short>();

    //Called when the the state of the controller changes.
    public Action<string> OnStatusUpdated;

    //Called after the user is done speaking and vosk processes the audio.
    public Action<string> OnTranscriptionResult;

    //The absolute path to the decompressed model folder.
    private string _decompressedModelPath;

    //A string that contains the keywords in Json Array format
    private string _grammar = "";

    //Flag that is used to wait for the model file to decompress successfully.
    private bool _isDecompressing;

    //Flag that is used to wait for the the script to start successfully.
    private bool _isInitializing;

    //Flag that is used to check if Vosk was started.
    private bool _didInit;

    //Threading Logic
    //Lock for the string result
    private object _resultLock = new object();
    //The json string that was returned from Vosk
    private string _threadedRecognitionResult;
    //The result that was called in the Recognition event.
    private string _result;
    //Thread safe queue of microphone data.
    private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();
    //lock for StreamingIsBusy flag.
    private int _threadSafeBoolBackValue = 0;
    //Flag to see if we are processing speech to text data.
    public bool StreamingIsBusy
    {
        get => (Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 1) == 1);
        set
        {
            if (value) Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 1, 0);
            else Interlocked.CompareExchange(ref _threadSafeBoolBackValue, 0, 1);
        }
    }

    void Start()
    {
        if (AutoStart)
        {
            StartVoskStt();
        }
    }

    public void StartVoskStt( List<string> keyPhrases= null, string modelPath = default, bool startMicrophone = true, int maxAlternatives = 3)
    {
        if (_isInitializing)
        {
            Debug.LogError("Initializing in progress!");
            return;
        }
        if (_didInit)
        {
            Debug.LogError("Vosk has already been initialized!");
            return;
        }

        if (!string.IsNullOrEmpty(modelPath))
        {
            ModelPath = modelPath;
        }

        if (keyPhrases != null)
        {
            KeyPhrases = keyPhrases;
        }

        MaxAlternatives = maxAlternatives;
        StartCoroutine(DoStartVoskStt(startMicrophone));
    }

    private IEnumerator DoStartVoskStt(bool startMicrophone)
    {
        _isInitializing = true;
        yield return WaitForMicrophoneInput();

        yield return Decompress();

        OnStatusUpdated?.Invoke("Loading Model from: " + _decompressedModelPath);
        Vosk.Vosk.SetLogLevel(0);
        _model = new Model(_decompressedModelPath);
    
        yield return null;

        OnStatusUpdated?.Invoke("Listening");
        VoiceProcessor.OnFrameCaptured += VoiceProcessorOnOnFrameCaptured;
        VoiceProcessor.OnRecordingStop += VoiceProcessorOnOnRecordingStop;
       
        if(startMicrophone)
            VoiceProcessor.StartRecording();

        _isInitializing = false;
        _didInit = true;

    }

    private void UpdateGrammar()
    {
        if (KeyPhrases.Count == 0)
        {
            _grammar = "";
            return;
        }
        
        JSONArray keywords = new JSONArray();
        foreach (string keyphrase in KeyPhrases)
        {
            keywords.Add(new JSONString(keyphrase.ToLower()));
        }
        
        keywords.Add(new JSONString("[unk]"));
        
        _grammar = keywords.ToString();
    }

    //Decompress the model zip file or return the location of the decompressed files.
    private IEnumerator Decompress()
    {
        if (!Path.HasExtension(ModelPath)
            || Directory.Exists(
                Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ModelPath))))
        {
            OnStatusUpdated?.Invoke("Using existing decompressed model.");
            _decompressedModelPath =
                Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(ModelPath));
            Debug.Log(_decompressedModelPath);

            yield break;
        }

        OnStatusUpdated?.Invoke("Decompressing model...");
        string dataPath = Path.Combine(Application.streamingAssetsPath, ModelPath);

        Stream dataStream;
        // Read data from the streaming assets path. You cannot access the streaming assets directly on Android.
        if (dataPath.Contains("://"))
        {
            UnityWebRequest www = UnityWebRequest.Get(dataPath);
            www.SendWebRequest();
            while (!www.isDone)
            {
                yield return null;
            }
            dataStream = new MemoryStream(www.downloadHandler.data);
        }
        // Read the file directly on valid platforms.
        else
        {
            dataStream = File.OpenRead(dataPath);
        }

        //Read the Zip File
        var zipFile = ZipFile.Read(dataStream);

        //Listen for the zip file to complete extraction
        zipFile.ExtractProgress += ZipFileOnExtractProgress;

        //Update status text
        OnStatusUpdated?.Invoke("Reading Zip file");

        //Start Extraction
        zipFile.ExtractAll(Application.persistentDataPath);

        //Wait until it's complete
        while (_isDecompressing == false)
        {
            yield return null;
        }

        //Update status text
        OnStatusUpdated?.Invoke("Decompressing complete!");
        //Wait a second in case we need to initialize another object.
        yield return new WaitForSeconds(1);
        //Dispose the zipfile reader.
        zipFile.Dispose();
    }

    ///The function that is called when the zip file extraction process is updated.
    private void ZipFileOnExtractProgress(object sender, ExtractProgressEventArgs e)
    {
        if (e.EventType == ZipProgressEventType.Extracting_AfterExtractAll)
        {
            _isDecompressing = true;
            _decompressedModelPath = e.ExtractLocation;
        }
    }

    //Wait until microphones are initialized
    private IEnumerator WaitForMicrophoneInput()
    {
        while (Microphone.devices.Length <= 0)
            yield return null;
    }

    //Callback from the voice processor when new audio is detected
    private void VoiceProcessorOnOnFrameCaptured(short[] samples)
    {
        //Only change the state if we are starting fresh
        if(StreamingIsBusy == false && _buffer.Count==0)
            OnStatusUpdated?.Invoke("Listening");

        _buffer.AddRange(samples);
    }

    //Callback from the voice processor when recording stops
    private void VoiceProcessorOnOnRecordingStop()
    {
        if (StreamingIsBusy)
            return;
        
        OnStatusUpdated?.Invoke("Fetching Result");
        StreamingIsBusy = true;

        _threadedBufferQueue.Enqueue(_buffer.ToArray());
        Task.Run(ThreadedWork).ConfigureAwait(false);

        _buffer.Clear();
    }

    void Update()
    {
        lock (_resultLock)
        {
            if (_result != _threadedRecognitionResult)
            {
                OnStatusUpdated?.Invoke("Received Result");
                _result = _threadedRecognitionResult;
                OnTranscriptionResult?.Invoke(_result);
            }
        }
    }

    private async Task ThreadedWork()
    {
        StreamingIsBusy = true;
        voskRecognizerCreateMarker.Begin();
        if (!_recognizerReady)
        {
            UpdateGrammar();

            //Only detect defined keywords if they are specified.
            if (string.IsNullOrEmpty(_grammar))
            {
                _recognizer = new VoskRecognizer(_model, 16000.0f);
            }
            else
            {
                _recognizer = new VoskRecognizer(_model, 16000.0f, _grammar);
            }

            _recognizer.SetMaxAlternatives(MaxAlternatives);
            _recognizer.SetWords(true);
            _recognizerReady = true;

            await Task.Delay(100);

        }

        voskRecognizerCreateMarker.End();

        voskRecognizerReadMarker.Begin();
        while (_threadedBufferQueue.Count > 0)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] voiceResult))
            {
                if (_recognizer.AcceptWaveform(voiceResult, voiceResult.Length))
                {
                    lock (_resultLock)
                    {
                        _threadedRecognitionResult = _recognizer.Result();
                    }
                }
                else
                {
                    lock (_resultLock)
                    {
                        _threadedRecognitionResult = _recognizer.PartialResult();
                    }
                }
            }
        }
       
        voskRecognizerReadMarker.End();

        await Task.Delay(2000);
 
        StreamingIsBusy = false;

    }

    static readonly ProfilerMarker voskRecognizerCreateMarker = new ProfilerMarker("VoskRecognizer.Create");
    static readonly ProfilerMarker voskRecognizerReadMarker = new ProfilerMarker("VoskRecognizer.AcceptWaveform");
  
}