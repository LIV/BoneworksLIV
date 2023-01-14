using UnityEngine;
using System;
using System.Runtime.InteropServices;
using LIVPathfinderSDK;

namespace LIV.SDK.Unity.Volumetric.GameSDK
{
    public enum PLUGIN_EVENT : int
    {
        CAPTURE_DEPTH = 1,
        CAPTURE_COLOR = 2,
        SUBMIT_TO_LIV = 3
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RequestedPose
    {
        public int frameID;
        public int width;
        public int height;
        public SDKMatrix4x4 projectionMatrix;
        public float nearClipPlane;
        public float farClipPlane;
        public float verticalFieldOfView;
        public SDKVector3 cameraPosition;
        public SDKQuaternion cameraRotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RequestedFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 128)]
        public RequestedPose[] cameras;
        public int cameraCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderPose
    {
        public int frameID;
        public int width;
        public int height;
        public SDKMatrix4x4 projectionMatrix;
        public float nearClipPlane;
        public float farClipPlane;
        public float verticalFieldOfView;
        public SDKVector3 cameraPosition;
        public SDKQuaternion cameraRotation;
        public SDKVector3 cameraLossyScale;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 128)]
        public RenderPose[] cameras;
        public int cameraCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ApplicationOutput
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string engineName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string engineVersion;
        [MarshalAs(UnmanagedType.LPStr)]
        public string applicationName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string applicationVersion;
        [MarshalAs(UnmanagedType.LPStr)]
        public string graphicsAPI;
        [MarshalAs(UnmanagedType.LPStr)]
        public string sdkID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string sdkVersion;
    }

    public static class SDKVolumetricBridge
    {
        [DllImport("LIV_VOLCAP")]
        public static extern IntPtr GetRenderEventFunc();

        // Is bridge loaded
        [DllImport("LIV_VOLCAP")]
        public static extern bool is_active();

        // Initializes the dll
        [DllImport("LIV_VOLCAP")]
        public static extern void create();

        // Cleans the dll
        [DllImport("LIV_VOLCAP")]
        public static extern void release();

        [DllImport("LIV_VOLCAP")]
        public static extern void set_application_output(ref ApplicationOutput applicationOutput);

        [DllImport("LIV_VOLCAP", CharSet = CharSet.Ansi)]
        public static extern void set_path([MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport("LIV_VOLCAP")]
        public static extern int get_requested_frame(ref RequestedFrame renderFrame);

        [DllImport("LIV_VOLCAP")]
        public static extern int submit_rendered_frame(ref RenderFrame renderFrame);

        [DllImport("LIV_VOLCAP", CharSet = CharSet.Ansi)]
        public static extern void set_frame_metadata([MarshalAs(UnmanagedType.LPStr)] string path);

        public const string LIV_VOLCAP_AVATAR_PATH = "avatar";
        public const string LIV_VOLCAP_GAME_PATH = "game";

        public static bool isActive => is_active();
        
        public static bool SetCameras(ref RequestedFrame requestedFrame, CameraSetup[] cameraSetups)
        {
            RenderFrame renderFrame = new RenderFrame();
            renderFrame.cameras = new RenderPose[128];
            renderFrame.cameraCount = requestedFrame.cameraCount;
            
            for (int i = 0; i < requestedFrame.cameraCount; i++)
            {
                renderFrame.cameras[i].frameID = Time.frameCount;
                renderFrame.cameras[i].width = requestedFrame.cameras[i].width;
                renderFrame.cameras[i].height = requestedFrame.cameras[i].height;
                renderFrame.cameras[i].projectionMatrix = requestedFrame.cameras[i].projectionMatrix;
                renderFrame.cameras[i].nearClipPlane = requestedFrame.cameras[i].nearClipPlane;
                renderFrame.cameras[i].farClipPlane = requestedFrame.cameras[i].farClipPlane;
                renderFrame.cameras[i].verticalFieldOfView = requestedFrame.cameras[i].verticalFieldOfView;
                renderFrame.cameras[i].cameraPosition = requestedFrame.cameras[i].cameraPosition;
                renderFrame.cameras[i].cameraRotation = requestedFrame.cameras[i].cameraRotation;
                renderFrame.cameras[i].cameraLossyScale = cameraSetups[i].lossyScale;
            }

            if (submit_rendered_frame(ref renderFrame) != 0) return false;
            return true;
        }

        public static bool GetRequestedFrame(ref RequestedFrame requestedFrame)
        {
            if (requestedFrame.cameras == null || requestedFrame.cameras.Length != 128)
            {
                requestedFrame.cameras = new RequestedPose[128];
            }
            if (get_requested_frame(ref requestedFrame) != 0) return false;
            return true;
        }
    }
}
