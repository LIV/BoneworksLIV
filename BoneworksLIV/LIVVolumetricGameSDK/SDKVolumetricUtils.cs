using System.Collections;
using System.Collections.Generic;
using Il2CppSystem;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LIV.SDK.Unity.Volumetric.GameSDK
{
    public static class SDKVolumetricUtils
    {
        public static CameraEvent GetCameraEvent(Camera camera)
        {
            switch (camera.renderingPath)
            {
                case RenderingPath.Forward:
                case RenderingPath.VertexLit:
                    return CameraEvent.AfterForwardOpaque;
                case RenderingPath.DeferredLighting:
                case RenderingPath.DeferredShading:
                    return CameraEvent.AfterGBuffer;
            }

            return CameraEvent.AfterEverything;
        }
        
        public static RenderTextureReadWrite GetReadWriteFromColorSpace(TEXTURE_COLOR_SPACE colorSpace)
        {
            switch (colorSpace)
            {
                case TEXTURE_COLOR_SPACE.LINEAR:
                    return RenderTextureReadWrite.Linear;
                case TEXTURE_COLOR_SPACE.SRGB:
                    return RenderTextureReadWrite.sRGB;
                default:
                    return RenderTextureReadWrite.Default;
            }
        }

        public static TEXTURE_COLOR_SPACE GetDefaultColorSpace {
            get {
                switch (QualitySettings.activeColorSpace)
                {
                    case UnityEngine.ColorSpace.Gamma:
                        return TEXTURE_COLOR_SPACE.SRGB;
                    case UnityEngine.ColorSpace.Linear:
                        return TEXTURE_COLOR_SPACE.LINEAR;

                }
                return TEXTURE_COLOR_SPACE.UNDEFINED;
            }
        }

        public static TEXTURE_COLOR_SPACE GetColorSpace(RenderTexture renderTexture)
        {
            if (renderTexture == null) return TEXTURE_COLOR_SPACE.UNDEFINED;
            if (renderTexture.sRGB) return TEXTURE_COLOR_SPACE.SRGB;
            return TEXTURE_COLOR_SPACE.LINEAR;
        }

        public static RENDERING_PIPELINE GetRenderingPipeline(RenderingPath renderingPath)
        {
            switch (renderingPath)
            {
                case RenderingPath.DeferredLighting:
                    return RENDERING_PIPELINE.DEFERRED;
                case RenderingPath.DeferredShading:
                    return RENDERING_PIPELINE.DEFERRED;
                case RenderingPath.Forward:
                    return RENDERING_PIPELINE.FORWARD;
                case RenderingPath.VertexLit:
                    return RENDERING_PIPELINE.VERTEX_LIT;
                default:
                    return RENDERING_PIPELINE.UNDEFINED;
            }
        }

        public static TEXTURE_DEVICE GetDevice()
        {
            switch (SystemInfo.graphicsDeviceType)
            {
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    return TEXTURE_DEVICE.DIRECTX;
                case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
                    return TEXTURE_DEVICE.VULKAN;
                case UnityEngine.Rendering.GraphicsDeviceType.Metal:
                    return TEXTURE_DEVICE.METAL;
                case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
                    return TEXTURE_DEVICE.OPENGL;
                default:
                    return TEXTURE_DEVICE.UNDEFINED;
            }
        }

        public static bool ContainsFlag(ulong flags, ulong flag)
        {
            return (flags & flag) != 0;
        }

        public static ulong SetFlag(ulong flags, ulong flag, bool enabled)
        {
            if (enabled)
            {
                return flags | flag;
            }
            else
            {
                return flags & (~flag);
            }
        }

        public static void GetCameraPositionAndRotation(RequestedPose pose, Matrix4x4 originLocalToWorldMatrix, out Vector3 position, out Quaternion rotation)
        {
            position = originLocalToWorldMatrix.MultiplyPoint(pose.cameraPosition);
            rotation = RotateQuaternionByMatrix(originLocalToWorldMatrix, pose.cameraRotation);
        }

        public static void CleanCameraBehaviours(Camera camera, string[] excludeBehaviours)
        {
            // Remove all children from camera clone.
            foreach (Transform child in camera.transform)
            {
                Object.Destroy(child.gameObject);
            }

            if (excludeBehaviours == null) return;
            for (int i = 0; i < excludeBehaviours.Length; i++)
            {
                Object.Destroy(camera.GetComponent(excludeBehaviours[i]));
            }
        }

        public static void SetCamera(Camera camera, Transform cameraTransform, RequestedPose pose, Matrix4x4 originLocalToWorldMatrix, int layerMask)
        {
            Vector3 worldPosition = Vector3.zero;
            Quaternion worldRotation = Quaternion.identity;
            float verticalFieldOfView = pose.verticalFieldOfView;
            float nearClipPlane = pose.nearClipPlane;
            float farClipPlane = pose.farClipPlane;
            Matrix4x4 projectionMatrix = pose.projectionMatrix;

            GetCameraPositionAndRotation(pose, originLocalToWorldMatrix, out worldPosition, out worldRotation);

            cameraTransform.position = worldPosition;
            cameraTransform.rotation = worldRotation;
            camera.fieldOfView = verticalFieldOfView;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            camera.projectionMatrix = projectionMatrix;
            camera.cullingMask = layerMask;
        }

        public static Quaternion RotateQuaternionByMatrix(Matrix4x4 matrix, Quaternion rotation)
        {
            return Quaternion.LookRotation(
                matrix.MultiplyVector(Vector3.forward),
                matrix.MultiplyVector(Vector3.up)
            ) * rotation;
        }

        public static SDKTrackedSpace GetTrackedSpace(Transform transform)
        {
            if (transform == null) return SDKTrackedSpace.empty;
            return new SDKTrackedSpace
            {
                trackedSpaceWorldPosition = transform.position,
                trackedSpaceWorldRotation = transform.rotation,
                trackedSpaceLocalScale = transform.localScale,
                trackedSpaceLocalToWorldMatrix = transform.localToWorldMatrix,
                trackedSpaceWorldToLocalMatrix = transform.worldToLocalMatrix,
            };
        }

        public static bool DestroyObject<T>(ref T reference) where T : UnityEngine.Object
        {
            if (reference == null) return false;
            Object.Destroy(reference);
            reference = default(T);
            return true;
        }

        public static bool DisposeObject<T>(ref T reference) where T : Il2CppSystem.Object
        {
            if (reference == null) return false;
            reference.Cast<IDisposable>().Dispose();
            reference = default(T);
            return true;
        }

        public static bool CreateTexture(ref RenderTexture renderTexture, int width, int height, int depth, RenderTextureFormat format)
        {
            DestroyTexture(ref renderTexture);
            if (width <= 0 || height <= 0)
            {
                Debug.LogError("LIV: Unable to create render texture. Texture dimension must be higher than zero.");
                return false;
            }

            renderTexture = new RenderTexture(width, height, depth, format)
            {
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                anisoLevel = 0
            };

            if (!renderTexture.Create())
            {
                Debug.LogError("LIV: Unable to create render texture.");
                return false;
            }

            return true;
        }

        public static void DestroyTexture(ref RenderTexture _renderTexture)
        {
            if (_renderTexture == null) return;
            if (_renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }
            _renderTexture = null;
        }

        // Disable standard assets if required.
        public static void DisableStandardAssets(Camera cameraInstance, ref MonoBehaviour[] behaviours, ref bool[] wasBehaviourEnabled)
        {
            behaviours = null;
            wasBehaviourEnabled = null;
            behaviours = cameraInstance.gameObject.GetComponents<MonoBehaviour>();
            wasBehaviourEnabled = new bool[behaviours.Length];
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                // generates garbage
                if (behaviour.enabled && behaviour.GetType().ToString().StartsWith("UnityStandardAssets."))
                {
                    behaviour.enabled = false;
                    wasBehaviourEnabled[i] = true;
                }
            }
        }

        // Restore disabled behaviours.
        public static void RestoreStandardAssets(ref MonoBehaviour[] behaviours, ref bool[] wasBehaviourEnabled)
        {
            if (behaviours != null)
                for (var i = 0; i < behaviours.Length; i++)
                    if (wasBehaviourEnabled[i])
                        behaviours[i].enabled = true;
        }
    }
}