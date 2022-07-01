using System.Runtime.InteropServices;

namespace MauiIosAudioRepro.Utils;

public class AudioUtils
{
    private static readonly int SizeOfShort = Marshal.SizeOf(typeof(short));
    private static readonly int SizeOfFloat = Marshal.SizeOf(typeof(float));

    public static short[] ConvertAudioByteArrayToShortArray(byte[] audio)
    {
        short[] output = new short[audio.Length / SizeOfShort];

        int arrayPosition = 0;
        for(var i = 0; i < audio.Length; i += SizeOfShort)
        {
            short sample = BitConverter.ToInt16(audio, i);
            output[arrayPosition] = sample;
        }

        return output;
    }    
    
    public static float[] ConvertAudioByteArrayToFloatArray(byte[] audio)
    {
        float[] output = new float[audio.Length / SizeOfFloat];

        int arrayPosition = 0;
        for(var i = 0; i < audio.Length; i += SizeOfFloat)
        {
            float sample = BitConverter.ToSingle(audio, i);
            output[arrayPosition] = sample;
        }

        return output;
    }

    public static (double, double) ConvertToShortArrayAndGetAudioRmsAndPeak(byte[] audio)
    {
        short[] samples = ConvertAudioByteArrayToShortArray(audio);

        return GetAudioRmsAndPeak(samples);
    }
    
    public static (double, double) ConvertToFloatArrayAndGetAudioRmsAndPeak(byte[] audio)
    {
        float[] samples = ConvertAudioByteArrayToFloatArray(audio);

        return GetAudioRmsAndPeak(samples);
    }
    
    public static (double, double) GetAudioRmsAndPeak(short[] audio)
    {
        double sumOfSampleSq = 0.0; // sum of square of normalized samples.
        double peakSample = 0.0; // peak sample.

        short normalizationValue = short.MaxValue;

        foreach (var sample in audio)
        {
            double normSample = (double)sample / normalizationValue; // normalized the sample with maximum value.
            sumOfSampleSq += (normSample * normSample);

            // se il sample è MinValue di short, non è possibile fare Abs, quindi lo trasformo direttamente in MaxValue
            var toBeAbsSample = sample == short.MinValue ? short.MaxValue : sample;
            var absSample = Math.Abs(toBeAbsSample);
            
            if (absSample > peakSample)
                peakSample = absSample;
        }

        double rms = 10 * Math.Log10(sumOfSampleSq / audio.Length);
        double peak = 20 * Math.Log10(peakSample / normalizationValue);

        return (rms, peak);
    }
    
    public static (double, double) GetAudioRmsAndPeak(float[] audio)
    {
        float sumOfSampleSq = 0.0f; // sum of square of normalized samples.
        float peakSample = 0.0f; // peak sample.

        float normalizationValue = float.MaxValue;

        foreach (var sample in audio)
        {
            var s = sample;
            if (float.IsNaN(sample)) s = 0;
            
            sumOfSampleSq += (s * s);
            
            var absSample = Math.Abs(s);
            
            if (absSample > peakSample)
                peakSample = absSample;
        }

        var squaredSum = Math.Sqrt(sumOfSampleSq / audio.Length);
        double rms = 10 * Math.Log10(squaredSum);

        
        //double rms = 10 * Math.Log10(sumOfSampleSq / audio.Length);
        double peak = 20 * Math.Log10(peakSample / normalizationValue);

        return (rms, peak);
    }
}