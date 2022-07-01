using AVFoundation;

namespace MauiIosAudioRepro.Platforms.iOS;

public class IosAudioRecordedEventArgs
{
    public AVAudioPcmBuffer Buffer { get; set; }
    public AVAudioTime AudioTime { get; set; }

    public IosAudioRecordedEventArgs(AVAudioPcmBuffer buffer, AVAudioTime audioTime)
    {
        Buffer = buffer;
        AudioTime = audioTime;
    }
}