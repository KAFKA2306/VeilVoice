using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace VeilVoice.Core
{
    public static class DeviceScanner
    {
        public static List<MMDevice> GetInputDevices()
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Cast<MMDevice>().ToList();
        }

        public static List<MMDevice> GetOutputDevices()
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Cast<MMDevice>().ToList();
        }

        public static MMDevice? FindVBCableInput()
        {
            return GetOutputDevices().FirstOrDefault(d => 
                d.FriendlyName.Contains("VeilVoiceOut") || 
                d.FriendlyName.Contains("CABLE Input") || 
                d.FriendlyName.Contains("Voicemeeter VAIO"));
        }

        public static MMDevice? FindBestInputDevice()
        {
            var inputs = GetInputDevices();
            var fifine = inputs.FirstOrDefault(d => d.FriendlyName.ToLower().Contains("fifine"));
            if (fifine != null) return fifine;
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console) ?? inputs.FirstOrDefault();
        }

        public static string GetEndpointDisclosure()
        {
            var vvo = FindVBCableInput();
            if (vvo == null) return "None";
            var info = new
            {
                Name = vvo.FriendlyName,
                Id = vvo.ID,
                State = vvo.State.ToString(),
                Provider = vvo.FriendlyName.Contains("Voicemeeter") ? "VB-Audio Software" : "Unknown"
            };
            return System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }
}
