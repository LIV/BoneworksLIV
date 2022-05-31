using UnityEngine;
using System;
using System.Runtime.InteropServices;
using LIV.Shared.Pathfinder;

namespace LIV.SDK.Unity.Volumetric
{
    public enum PLUGIN_EVENT : int
    {
        CAPTURE_DEPTH = 1,
        CAPTURE_COLOR = 2,
        SUBMIT_TO_LIV = 3,
        UPDATE_LIV_GET_FRAME = 4
    }
    
    public struct SDKCamera
    {
        public SDKPose pose;
        public SDKResolution resolution;
    }

    public static class SDKVolumetricBridge
    {
        // Native plugin rendering events are only called if a plugin is used
        // by some script. This means we have to DllImport at least
        // one function in some active script.
        // For this example, we'll call into plugin's SetTimeFromUnity
        // function and pass the current time so the plugin can animate.

        [DllImport("BONEWORKS_Data/Plugins/LIV_VOLCAP.dll")]
        public static extern IntPtr GetRenderEventFunc();

        [DllImport("BONEWORKS_Data/Plugins/LIV_VOLCAP.dll")]
        public static extern IntPtr SetCameraCount(int count);
        
        [DllImport("BONEWORKS_Data/Plugins/LIV_VOLCAP.dll")]
        public static extern void Create();

        [DllImport("BONEWORKS_Data/Plugins/LIV_VOLCAP.dll")]
        public static extern void Release();

        public static bool isActive
        {
            get { return true; }
        }

        const string SDK_PATH_PREFIX = "LIV.VOLCAPPOSE.";
        
        public static bool GetCameras(ref SDKCamera[] cameras)
        {
            int cameraCount = 2;
            SDKResolution resolution;
            
            cameras = new SDKCamera[cameraCount];
            for (int i = 0; i < cameraCount; i++)
            {
                int poseID = i;
                SDKPose pose = new SDKPose();
                string CAMERAS_PATH = $"{SDK_PATH_PREFIX}";
                string projectionMatrixPath = $"{CAMERAS_PATH}camera{poseID}.projectionmatrix";
                if (!SDKBridgeVolcap.GetValue<SDKMatrix4x4>(projectionMatrixPath,
                    out pose.projectionMatrix, (int)PathfinderType.Matrix4x4))
                {
                    Debug.LogError($"Cant get value: projectionmatrix from: {projectionMatrixPath}");
                    pose.projectionMatrix = Matrix4x4.identity;
                }

                string positionLocalPath = $"{CAMERAS_PATH}camera{poseID}.positionlocal";
                if (!SDKBridgeVolcap.GetValue<SDKVector3>(positionLocalPath, out pose.localPosition,
                    (int)PathfinderType.Vector3))
                {
                    Debug.LogError($"Cant get value: positionlocal from: {positionLocalPath}");
                    pose.localPosition = Vector3.zero;
                }

                string rotationLocalPath = $"{CAMERAS_PATH}camera{poseID}.rotationlocal";
                if (!SDKBridgeVolcap.GetValue<SDKQuaternion>(rotationLocalPath, out pose.localRotation,
                    (int)PathfinderType.Quaternion))
                {
                    Debug.LogError($"Cant get value: rotationlocal from: {rotationLocalPath}");
                    pose.localRotation = Quaternion.identity;
                }

                string nearClipPlanePath = $"{CAMERAS_PATH}camera{poseID}.nearclipplane";
                if (!SDKBridgeVolcap.GetValue<float>(nearClipPlanePath, out pose.nearClipPlane,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: nearclipplane fromt: {nearClipPlanePath}");
                    pose.nearClipPlane = 0.1f;
                }

                string farClipPlanePath = $"{CAMERAS_PATH}camera{poseID}.farclipplane";
                if (!SDKBridgeVolcap.GetValue<float>(farClipPlanePath, out pose.farClipPlane,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: farclipplane from: {farClipPlanePath}");
                    pose.farClipPlane = 1000f;
                }

                string verticalFovPath = $"{CAMERAS_PATH}camera{poseID}.verticalfov";
                if (!SDKBridgeVolcap.GetValue<float>(verticalFovPath, out pose.verticalFieldOfView,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: verticalfov from: {verticalFovPath}");
                    pose.verticalFieldOfView = 60f;
                }

                string widthPath = $"{CAMERAS_PATH}camera{poseID}.width";
                SDKBridgeVolcap.GetValue<int>(widthPath, out resolution.width,
                    (int)PathfinderType.Int);
        
                string heightPath = $"{CAMERAS_PATH}camera{poseID}.height";
                SDKBridgeVolcap.GetValue<int>(heightPath, out resolution.height,
                    (int)PathfinderType.Int);

                cameras[i] = new SDKCamera()
                {
                    pose = pose,
                    resolution = resolution
                };
            }

            return true;
        }
    }
}
