using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LIVPathfinderSDK;
using MelonLoader;
using UnityEngine;

namespace LIV
{
    public class WasapiCapture : MonoBehaviour
    {
        public WasapiCapture(IntPtr ptr) : base(ptr)
        {
        }
        
        [DllImport("LIV.WasapiCapture")]
        private static extern UInt64 LIV_WasapiCapture_init();
        private UInt64 CaputureHandle = 0;

        [DllImport("LIV.WasapiCapture")]
        private static extern void LIV_WasapiCapture_close(UInt64 Handle);

        [DllImport("LIV.WasapiCapture")]
        private static extern bool LIV_WasapiCapture_Hook(UInt64 Handle);

        [DllImport("LIV.WasapiCapture")]
        private static extern Int64 LIV_WasapiCapture_GetAvailableCaptureFrameCount(UInt64 Handle);

        [DllImport("LIV.WasapiCapture")]
        private static extern Int64 LIV_WasapiCapture_GetNextFrameSize(UInt64 Handle);

        [DllImport("LIV.WasapiCapture")]
        private static extern bool LIV_WasapiCapture_GetData(UInt64 Handle, IntPtr output, Int64 outputSize);

        // Start is called before the first frame update
        void OnEnable()
        {
            CaputureHandle = LIV_WasapiCapture_init();

            if (!LIV_WasapiCapture_Hook(CaputureHandle))
            {
                Debug.LogError("Failed to hook WASAPI AudioClient");
            }
        }

        void OnDisable()
        {
            LIV_WasapiCapture_close(CaputureHandle);
        }

        private float[] _audioData;

        void ProcessAudio(float[] audioData)
        {
            _audioData = audioData;
            SDKPathfinder.SetPFValue("LIV.audio", ref audioData, audioData.Length);
        }

        void Update()
        {
            PumpAudio();
        }

        private void PumpAudio()
        {
            var availableCapture = LIV_WasapiCapture_GetAvailableCaptureFrameCount(CaputureHandle);

            for (int i = 0; i < availableCapture; i++)
            {
                var sampleCount = (int)LIV_WasapiCapture_GetNextFrameSize(CaputureHandle);
                var bufferSize = 4 * sampleCount;

                var ptr = Marshal.AllocHGlobal(bufferSize);

                var samples = new float[sampleCount];
                if (LIV_WasapiCapture_GetData(CaputureHandle, ptr, sampleCount))
                {
                    Marshal.Copy(ptr, samples, 0, sampleCount);
                    ProcessAudio(samples);
                }

                Marshal.FreeHGlobal(ptr);
            }
        }
    }


}
