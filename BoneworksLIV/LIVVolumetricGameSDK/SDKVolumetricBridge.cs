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
        private const string DllPath = "LIV_VOLCAP.dll";

        [DllImport(DllPath)]
        public static extern IntPtr GetRenderEventFunc();

        // Is bridge loaded
        [DllImport(DllPath)]
        public static extern bool IsBridgeLoaded();

        // Initializes the dll
        [DllImport(DllPath)]
        public static extern void Create();

        // Cleans the dll
        [DllImport(DllPath)]
        public static extern void Release();

        // Set path
        [DllImport(DllPath, CharSet = CharSet.Ansi)]
        public static extern void SetPath([MarshalAs(UnmanagedType.LPStr)] string path);

        const string FRAME_CAMERAS_PATH = "frame.cameras";
        const string LIV_VOLCAPPOSE_PATH = "LIV.VOLCAPPOSE";
        static string LIV_VOLCAPPOSE_CAMERAS_PATH = $"{LIV_VOLCAPPOSE_PATH}.cameras";

        public static bool isActive
        {
            get
            {
                if(!IsBridgeLoaded()) return false;
                if (!SDKPathfinder.GetPFValue<int>($"{LIV_VOLCAPPOSE_PATH}.active", out int volcapActive, (int)PathfinderType.Int)) return false;
                return volcapActive == 1;
            }
        }

        public static int GetCameraCount()
        {
            if (!IsBridgeLoaded())
            {
                Debug.LogError("Bridge was not loaded!");
                return 0;
            }

            if (!SDKPathfinder.GetPFValue<int>($"{LIV_VOLCAPPOSE_PATH}.cameracount", out int cameraCount,
                (int)PathfinderType.Int)) return 0;
            return cameraCount;
        }

        public static bool SetCameraCount(int cameraCount)
        {
            if (!IsBridgeLoaded())
            {
                Debug.LogError("Bridge was not loaded!");
                return false;
            }

            if (!SDKPathfinder.SetPFValue<int>("frame.cameracount", ref cameraCount,
                (int)PathfinderType.Int)) return false;
            return true;
        }

        public static bool SetCamera(int id, SDKCamera camera, SDKVector3 cameraLossyScale)
        {
            if (!IsBridgeLoaded())
            {
                Debug.LogError("Bridge was not loaded!");
                return false;
            }
            int frameID = Time.frameCount;
            SDKPathfinder.SetPFValue<int>($"{FRAME_CAMERAS_PATH}.camera{id}.frameid", ref frameID, (int)PathfinderType.Int);
            SDKPathfinder.SetPFValue<int>($"{FRAME_CAMERAS_PATH}.camera{id}.width", ref camera.resolution.width, (int)PathfinderType.Int);
            SDKPathfinder.SetPFValue<int>($"{FRAME_CAMERAS_PATH}.camera{id}.height", ref camera.resolution.height, (int)PathfinderType.Int);
            SDKPathfinder.SetPFValue<SDKMatrix4x4>($"{FRAME_CAMERAS_PATH}.camera{id}.projectionmatrix", ref camera.pose.projectionMatrix, (int)PathfinderType.Matrix4x4);
            SDKPathfinder.SetPFValue<SDKVector3>($"{FRAME_CAMERAS_PATH}.camera{id}.positionlocal", ref camera.pose.localPosition, (int)PathfinderType.Vector3);
            SDKPathfinder.SetPFValue<SDKQuaternion>($"{FRAME_CAMERAS_PATH}.camera{id}.rotationlocal", ref camera.pose.localRotation, (int)PathfinderType.Quaternion);
            SDKPathfinder.SetPFValue<SDKVector3>($"{FRAME_CAMERAS_PATH}.camera{id}.lossyscale", ref cameraLossyScale, (int)PathfinderType.Vector3);
            SDKPathfinder.SetPFValue<float>($"{FRAME_CAMERAS_PATH}.camera{id}.nearclipplane", ref camera.pose.nearClipPlane, (int)PathfinderType.Float);
            SDKPathfinder.SetPFValue<float>($"{FRAME_CAMERAS_PATH}.camera{id}.farclipplane", ref camera.pose.farClipPlane, (int)PathfinderType.Float);
            SDKPathfinder.SetPFValue<float>($"{FRAME_CAMERAS_PATH}.camera{id}.verticalfov", ref camera.pose.verticalFieldOfView, (int)PathfinderType.Float);
            return true;
        }

        public static bool GetCameras(ref SDKCamera[] cameras)
        {
            if (!IsBridgeLoaded())
            {
                Debug.LogError("Bridge was not loaded!");
                return false;
            }
            int cameraCount = GetCameraCount();
            SDKResolution resolution;
            
            cameras = new SDKCamera[cameraCount];
            for (int i = 0; i < cameraCount; i++)
            {
                int poseID = i;
                SDKPose pose = new SDKPose();
                string projectionMatrixPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.projectionmatrix";
                if (!SDKPathfinder.GetPFValue<SDKMatrix4x4>(projectionMatrixPath,
                    out pose.projectionMatrix, (int)PathfinderType.Matrix4x4))
                {
                    Debug.LogError($"Cant get value: projectionmatrix from: {projectionMatrixPath}");
                    pose.projectionMatrix = Matrix4x4.identity;
                }

                string positionLocalPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.positionlocal";
                if (!SDKPathfinder.GetPFValue<SDKVector3>(positionLocalPath, out pose.localPosition,
                    (int)PathfinderType.Vector3))
                {
                    Debug.LogError($"Cant get value: positionlocal from: {positionLocalPath}");
                    pose.localPosition = Vector3.zero;
                }

                string rotationLocalPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.rotationlocal";
                if (!SDKPathfinder.GetPFValue<SDKQuaternion>(rotationLocalPath, out pose.localRotation,
                    (int)PathfinderType.Quaternion))
                {
                    Debug.LogError($"Cant get value: rotationlocal from: {rotationLocalPath}");
                    pose.localRotation = Quaternion.identity;
                }

                string nearClipPlanePath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.nearclipplane";
                if (!SDKPathfinder.GetPFValue<float>(nearClipPlanePath, out pose.nearClipPlane,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: nearclipplane fromt: {nearClipPlanePath}");
                    pose.nearClipPlane = 0.1f;
                }

                string farClipPlanePath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.farclipplane";
                if (!SDKPathfinder.GetPFValue<float>(farClipPlanePath, out pose.farClipPlane,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: farclipplane from: {farClipPlanePath}");
                    pose.farClipPlane = 1000f;
                }

                string verticalFovPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.verticalfov";
                if (!SDKPathfinder.GetPFValue<float>(verticalFovPath, out pose.verticalFieldOfView,
                    (int)PathfinderType.Float))
                {
                    Debug.LogError($"Cant get value: verticalfov from: {verticalFovPath}");
                    pose.verticalFieldOfView = 60f;
                }

                string widthPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.width";
                SDKPathfinder.GetPFValue<int>(widthPath, out resolution.width,
                    (int)PathfinderType.Int);
        
                string heightPath = $"{LIV_VOLCAPPOSE_CAMERAS_PATH}.camera{poseID}.height";
                SDKPathfinder.GetPFValue<int>(heightPath, out resolution.height,
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
