using System;
using VeilVoice.Core;

namespace VeilVoiceAcceptanceRunner
{
    public static class ManualVerify
    {
        public static void CheckFifinePriority()
        {
            Console.WriteLine("=== Verifying Fifine Priority ===");
            var bestMic = DeviceScanner.FindBestInputDevice();
            if (bestMic != null)
            {
                Console.WriteLine($"Selected Mic: {bestMic.FriendlyName}");
                if (bestMic.FriendlyName.ToLower().Contains("fifine"))
                {
                    Console.WriteLine("SUCCESS: Fifine prioritized.");
                }
                else
                {
                    Console.WriteLine("INFO: Fifine not found, fell back to default.");
                }
            }
            else
            {
                Console.WriteLine("ERROR: No input devices found.");
            }
        }
    }
}
