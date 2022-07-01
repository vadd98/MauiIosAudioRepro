#if IOS
using MauiIosAudioRepro.Platforms.iOS;
#endif

namespace MauiIosAudioRepro;

public partial class MainPage : ContentPage
{
    #if IOS
    private IosRecorderService RecorderService = new IosRecorderService();
    #endif
    
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        await Permissions.RequestAsync<Permissions.Microphone>();
        await Permissions.RequestAsync<Permissions.Speech>();
        await Permissions.RequestAsync<Permissions.Media>();

        #if IOS
        RecorderService.StartRecording();
        #endif
    }
}