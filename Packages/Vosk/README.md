# About

Implementation of the Vosk offline speech to text (STT) library inside of Unity. I created this just to see if it could be done. Contributions are welcomed!

**Tested Using**
* Unity 2020.3+
* Windows 64
* Android Arm64

**Notes**
* Samples Include external packages. See the Third Part Notices for license information. 
* Core files are included under the `Packages` folder.
* Models can be found on the [Vosk Project website.](https://alphacephei.com/vosk/models)

# What is Vosk
[Vosk](https://github.com/alphacep/vosk-api) is an offline open source speech recognition toolkit. It enables speech recognition models for 18 languages and dialects - English, Indian English, German, French, Spanish, Portuguese, Chinese, Russian, Turkish, Vietnamese, Italian, Dutch, Catalan, Arabic, Greek, Farsi, Filipino, Ukrainian.

[Vosk models](https://alphacephei.com/vosk/models) are small (50 Mb) but provide continuous large vocabulary transcription, zero-latency response with streaming API, reconfigurable vocabulary and speaker identification.

Speech recognition bindings implemented for various programming languages like Python, Java, Node.JS, C#, C++ and others.

Vosk supplies speech recognition for chatbots, smart home appliances, virtual assistants. It can also create subtitles for movies, transcription for lectures and interviews.

Vosk scales from small devices like Raspberry Pi or Android smartphone to big clusters.

[Learn more](https://alphacephei.com/vosk/) 

#  Sample Scripts/Scene Info
The DemoScene can be run on android or standalone. 
#### Controls
Launch the scene and begin speaking, next should appear on after you spot speaking for two seconds. -- Minimum Volume may need to be changed if you are in an noisy environment.
#### Script Logic
The Controller GameObject has two components that are responsible for the transcription.

**Voice Processor** - Handles the microphone input. Microphone can be enable and disabled. Auto detection can be disabled by passing `false` when calling  the `StartRecording` method.

**VoskSpeechToText** - Passes the microphone input into Vosk. If KeyPhrases are not assigned, the recognizer will recognize all words in the Models dictionary. The detection starts by default but can be started by hand by calling `StartVoskSTT`.

*Notes*  
Streaming voice detection is not implemented.
Models should be included in StreamingAssets as a zip or directory.