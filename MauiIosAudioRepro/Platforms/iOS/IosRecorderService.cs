using System.Runtime.InteropServices;
using AudioToolbox;
using AVFoundation;
using MauiIosAudioRepro.Utils;

namespace MauiIosAudioRepro.Platforms.iOS;

public class IosRecorderService 
{
    private AVAudioEngine _audioEngine = null;
    private AVAudioMixerNode _recognitionMixerNode = null;
    private AVAudioMixerNode _levelDetectionMixerNode = null;
    private AVAudioConverter _audioConverter = null;
    
    private readonly AVAudioFormat _audioFormat16Bit = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, 
        44100, 1, false);    
    
    private readonly AVAudioFormat _audioFormatOutput = new AVAudioFormat(44100, 1);

    public event EventHandler<IosAudioRecordedEventArgs> AudioRecorded;
    public event EventHandler<PeakDetectedEventArgs> PeakDetected;
    public event EventHandler CantRecord;

    // i due seguenti sono usati per evitare che CantStartRecording venga invocato sia da RecordSampleAsync che da RecordAudioAsync
    object _cantRecord = new();
    bool InvokedCantRecord { get; set; }

    private bool _isRecording = false;
    public bool IsRecording { get => _isRecording; private set => _isRecording = value; }
    



    public void StartRecording()
    {
        if (IsRecording)
        {
            return;
        }

        IsRecording = true;
        InvokedCantRecord = false;

        _audioEngine = new AVAudioEngine();
        _recognitionMixerNode = new AVAudioMixerNode();
        _levelDetectionMixerNode = new AVAudioMixerNode();

        var audioSession = AVAudioSession.SharedInstance();
        audioSession.SetCategory(AVAudioSession.CategoryRecord);
        audioSession.SetActive(true);

        RecognizeAudioFromMicrophoneInBackground();
        
        audioSession.RequestRecordPermission(response =>
        {
            if (response)
            {
                _audioEngine.Prepare();
                _audioEngine.StartAndReturnError(out _);
            }
        });
    }

    public void StopRecording()
    {
        if (!IsRecording)
        {
            return;
        }

        IsRecording = false;

        if (_audioEngine != null && _audioEngine.Running)
        {
            _recognitionMixerNode.RemoveTapOnBus(0);
            _levelDetectionMixerNode.RemoveTapOnBus(0);
            _audioEngine.Stop();
            
            _audioEngine?.Dispose();
            _recognitionMixerNode?.Dispose();
            _levelDetectionMixerNode?.Dispose();
            _audioConverter?.Dispose();

            _audioEngine = null;
            _recognitionMixerNode = null;
            _levelDetectionMixerNode = null;
            _audioConverter = null;
        }
    }

    private void RecognizeAudioFromMicrophoneInBackground()
    {
        var inputFormat = _audioEngine.InputNode.GetBusInputFormat(0);
        var outputFormat = _audioFormatOutput;

        // preparo un convertitore per convertire a 16bit
        _audioConverter = new AVAudioConverter(outputFormat, _audioFormat16Bit);

        // si può impostare un solo tap per bus, quindi creo due MixerNode

        // mixer riconoscimento
        _audioEngine.AttachNode(_recognitionMixerNode);
        _audioEngine.Connect(_audioEngine.InputNode, _recognitionMixerNode, inputFormat);
        _audioEngine.Connect(_recognitionMixerNode, _audioEngine.MainMixerNode, outputFormat);

        // mixer livello volume
        _audioEngine.AttachNode(_levelDetectionMixerNode);
        _audioEngine.Connect(_audioEngine.InputNode, _levelDetectionMixerNode, inputFormat);
        _audioEngine.Connect(_levelDetectionMixerNode, _audioEngine.MainMixerNode, outputFormat);

        _recognitionMixerNode.InstallTapOnBus(0, 8192, outputFormat,
            (buffer, audioTime) =>
            {
                IosAudioRecordedEventArgs eventArgs = new IosAudioRecordedEventArgs(buffer, audioTime);
                AudioRecorded?.Invoke(null, eventArgs);
            });

        _levelDetectionMixerNode.InstallTapOnBus(0, 4096, outputFormat, 
            GetAudioPeaksInBackground);
    }
    
    private void GetAudioPeaksInBackground(AVAudioPcmBuffer buffer, AVAudioTime audioTime)
    {
        byte[] bytes = ConvertAVAudioPcmBufferToBytes(buffer);
        float[] floats = AudioUtils.ConvertAudioByteArrayToFloatArray(bytes);
        var (rms, peak) = AudioUtils.GetAudioRmsAndPeak(floats);

        float[] floatAudioArray = new float[buffer.FrameLength];
        Marshal.Copy(buffer.FloatChannelData, floatAudioArray, 0, floatAudioArray.Length);
        var (floatRms, floatPeak) = AudioUtils.GetAudioRmsAndPeak(floatAudioArray);
        Console.WriteLine("Float RMS: " + floatRms);
        Console.WriteLine("Float Peak: " + floatPeak);

        buffer.Dispose();
        audioTime.Dispose();
        
        PeakDetectedEventArgs eventArgs = new PeakDetectedEventArgs(rms, peak);
        PeakDetected?.Invoke(null, eventArgs);
    }
    
    private byte[] ConvertAVAudioPcmBufferToBytes(AVAudioPcmBuffer buffer)
    {
        AudioBuffer audioBuffer = buffer.AudioBufferList[0];
        byte[] data = new byte[audioBuffer.DataByteSize];
        Marshal.Copy(audioBuffer.Data, data, 0, audioBuffer.DataByteSize);
        return data;
    }
}