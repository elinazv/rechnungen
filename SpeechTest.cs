using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.IO;

public class SpeechTest
{
    public static async Task Run()
    {
        // Load configuration from appsettings.json in the current directory
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var speechConfig = configuration.GetSection("AzureSpeech");
        var subscriptionKey = speechConfig["SubscriptionKey"];
        var region = speechConfig["Region"];
        var language = speechConfig["Language"];

        if (string.IsNullOrEmpty(subscriptionKey) || string.IsNullOrEmpty(region))
        {
            Console.WriteLine("Error: Azure Speech configuration is missing. Please check appsettings.json");
            return;
        }

        var config = SpeechConfig.FromSubscription(subscriptionKey, region);
        config.SpeechRecognitionLanguage = language ?? "de-DE";
        var audio = AudioConfig.FromDefaultMicrophoneInput();
        var recognizer = new SpeechRecognizer(config, audio);

        Console.WriteLine("Speak now...");
        var result = await recognizer.RecognizeOnceAsync();
        Console.WriteLine(result.Text);
    }
}