using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using LIVPathfinderSDK;

namespace LIV.SDK.Unity.Volumetric.GameSDK
{
    public class SDKAudioCapture : IDisposable
    {
        private VolumetricGameSDK _volumetricGameSDK;
        private static LIVAudioCapture _livAudioCapture;
        public static LIVAudioCapture instance => _livAudioCapture;

        public SDKAudioCapture(VolumetricGameSDK volumetricGameSDK)
        {
            _volumetricGameSDK = volumetricGameSDK;
            CreateAssets();
        }

        public void Dispose()
        {
            DestroyAssets();
        }

        public void Capture()
        {
            // TODO: write audio metadata
            /*
            if (_audioCapture != null)
            {
                SDKAudioFrame sdkAudioFrame = new SDKAudioFrame();
                sdkAudioFrame.channels = 2;
                sdkAudioFrame.samplerate = AudioSettings.outputSampleRate;                
            }
            */
            //Debug.Log(AudioSettings.outputSampleRate);
        }

        void CreateAssets()
        {
            if (_volumetricGameSDK.audioListener == null) return;
            _livAudioCapture = _volumetricGameSDK.audioListener.gameObject.AddComponent<LIVAudioCapture>();
        }
        
        void DestroyAssets()
        {
            if (_livAudioCapture != null)
            {
                GameObject.Destroy(_livAudioCapture);
                _livAudioCapture = null;
            }
        }
    }
    
    public class LIVAudioCapture : MonoBehaviour
    {
        private void OnEnable()
        {
            if (SDKAudioCapture.instance != null && SDKAudioCapture.instance != this)
            {
                Destroy(this);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if(SDKAudioCapture.instance != this) return;
            if (!SDKVolumetricBridge.isActive) return;
            // TODO: bridge make sure data does not overflow!
            // Make sure the buffer does not overflow for now!
            Debug.Assert(data.Length < 15000);
            int length = Math.Min(data.Length, 15000);
            if (length < 1) return;
            SDKPathfinder.SetPFValue("LIV.audio", ref data, length);
        }
    }
}
