using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.IO;

public class SpeechTest
{
    public static async Task<string> Run()
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
            return "Not recognized";
        }

        var config = SpeechConfig.FromSubscription(subscriptionKey, region);
        config.SpeechRecognitionLanguage = language ?? "de-DE";
        var audio = AudioConfig.FromDefaultMicrophoneInput();
        var recognizer = new SpeechRecognizer(config, audio);

        Console.WriteLine("Speak now...");
        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"Recognized: {result.Text}");
            return result.Text;
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("No speech could be recognized.");
            return "Not recognized";
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            Console.WriteLine($"Error: {cancellation.Reason}");
            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"Error details: {cancellation.ErrorDetails}");
            }
            return "Not recognized";
        }

        return "Not recognized";
    }
}