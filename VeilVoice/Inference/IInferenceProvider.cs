using System;

namespace VeilVoice.Inference
{



    public interface IInferenceProvider : IDisposable
    {



        string EngineName { get; }






        float[] Process(float[] input);




        int LatencySamples { get; }




        bool IsReady { get; }
    }
}
