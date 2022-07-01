namespace MauiIosAudioRepro.Utils
{
    public class PeakDetectedEventArgs : EventArgs
    {
        public PeakDetectedEventArgs(double average, double peak)
        {
            Average = average;
            Peak = peak;
        }

        public double Average { get; }
        public double Peak { get; }
    }
}