using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace VeilVoice.Core
{
    public static class DeviceScanner
    {
        public static List<MMDevice> GetInputDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            return devices.Cast<MMDevice>().ToList();
        }

        public static List<MMDevice> GetOutputDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            return devices.Cast<MMDevice>().ToList();
        }

        public static MMDevice? FindVBCableInput()
        {
            var devices = GetOutputDevices();
            // Contract Option B: Use existing signed driver (VB-Audio/Voicemeeter)
            return devices.FirstOrDefault(d => 
                d.FriendlyName.Contains("VeilVoiceOut") || 
                d.FriendlyName.Contains("CABLE Input") || 
                d.FriendlyName.Contains("Voicemeeter VAIO"));
        }




        public static MMDevice FindBestInputDevice()
        {
            var inputs = GetInputDevices();
            

            var fifine = inputs.FirstOrDefault(d => d.FriendlyName.ToLower().Contains("fifine"));
            if (fifine != null) return fifine;


            try {
                var enumerator = new MMDeviceEnumerator();
                return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            } catch {
                return inputs.FirstOrDefault();
            }
        }
    }
}
