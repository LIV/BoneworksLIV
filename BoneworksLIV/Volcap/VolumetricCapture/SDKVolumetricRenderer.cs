using System;
using System.Collections;
using System.Collections.Generic;
using LIV.Shared.Pathfinder;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LIV.SDK.Unity.Volumetric
{
	public class CameraSetup : IDisposable
	{
		private int _id;
		private SDKVolumetricRenderer _renderer = null;
		private Camera _cameraInstance;
		private RenderTexture _colorTexture;
		public RenderTexture colorTexture{
			get { return _colorTexture; }
		}
		
		private CommandBuffer _livCaptureDepthCommandBuffer;
		private CommandBuffer _livCaptureColorCommandBuffer;
		private CameraEvent _captureDepthEvent = CameraEvent.AfterEverything;
		private CameraEvent _captureColorEvent = CameraEvent.AfterEverything;
		
		public CameraSetup(SDKVolumetricRenderer renderer, int id)
		{
			_id = id;
			_renderer = renderer;
			GameObject cloneGO = (GameObject)Object.Instantiate(_renderer.cameraReference.gameObject, _renderer.stage);
			_cameraInstance = (Camera)cloneGO.GetComponent("Camera");
			SDKVolumetricUtils.CleanCameraBehaviours(_cameraInstance, _renderer.excludeBehaviours);

			_cameraInstance.name = "Volumetric Camera "+_id;
			if (_cameraInstance.CompareTag("MainCamera"))
			{
				_cameraInstance.tag = "Untagged";
			}

			_cameraInstance.transform.localScale = Vector3.one;
			_cameraInstance.rect = new Rect(0, 0, 1, 1);
			_cameraInstance.depth = 0;
            _cameraInstance.stereoTargetEye = StereoTargetEyeMask.None;
            _cameraInstance.allowMSAA = false;
#if UNITY_5_4_OR_NEWER
			_cameraInstance.stereoTargetEye = StereoTargetEyeMask.None;
#endif
#if UNITY_5_6_OR_NEWER
            _cameraInstance.allowMSAA = false;
#endif
			int internalID = _id * 10;
			_livCaptureDepthCommandBuffer = new CommandBuffer();
			_livCaptureDepthCommandBuffer.name = "LIV.CaptureDepth"+_id;
			_livCaptureDepthCommandBuffer.IssuePluginEvent(SDKVolumetricBridge.GetRenderEventFunc(), internalID + (int)PLUGIN_EVENT.CAPTURE_DEPTH);
			
			_livCaptureColorCommandBuffer = new CommandBuffer();
			_livCaptureColorCommandBuffer.name = "LIV.CaptureColor"+_id;
			_livCaptureColorCommandBuffer.IssuePluginEvent(SDKVolumetricBridge.GetRenderEventFunc(), internalID + (int)PLUGIN_EVENT.CAPTURE_COLOR);
			
			_cameraInstance.enabled = false;
			_cameraInstance.gameObject.SetActive(true);
		}

		private void UpdateTextures(SDKResolution resolution)
		{
			if (
				_colorTexture == null ||
				_colorTexture.width != resolution.width ||
				_colorTexture.height != resolution.height
			)
			{
				if (SDKVolumetricUtils.CreateTexture(ref _colorTexture, resolution.width, resolution.height, 24, RenderTextureFormat.ARGB32))
				{
#if UNITY_EDITOR
					_colorTexture.name = "LIV.VolumetricColorRenderTexture"+_id;
#endif
				}
				else
				{
					Debug.LogError("LIV: Unable to create volumetric color texture! "+_id);
				}
			}
		}

		public void Render(SDKCamera camera)
		{
			string CAMERAS_PATH = "frame.cameras.";
			if (camera.resolution.width < 1 ||
			    camera.resolution.height < 1)
			{
				Debug.LogError($"LIV: Camera: {_id} resolution: {camera.resolution.width}x{camera.resolution.height} is invalid. ");
				return;
			}
			
			UpdateTextures(camera.resolution);
			if (_colorTexture != null)
			{
				_captureDepthEvent = SDKVolumetricUtils.GetCameraEvent(_cameraInstance);
				_cameraInstance.targetTexture = _colorTexture;
				SDKVolumetricUtils.SetCamera(_cameraInstance, _cameraInstance.transform, camera.pose, _renderer.localToWorldMatrix, _renderer.spectatorLayerMask);
				_cameraInstance.AddCommandBuffer(_captureDepthEvent, _livCaptureDepthCommandBuffer);
				_cameraInstance.AddCommandBuffer(_captureColorEvent, _livCaptureColorCommandBuffer);
				_cameraInstance.Render();
				_cameraInstance.RemoveCommandBuffer(_captureColorEvent, _livCaptureColorCommandBuffer);
				_cameraInstance.RemoveCommandBuffer(_captureDepthEvent, _livCaptureDepthCommandBuffer);
			}

			SDKVector3 cameraLossyScale = _cameraInstance.transform.lossyScale;

			int frameID = Time.frameCount;
			SDKBridgeVolcap.SetValue<int>($"{CAMERAS_PATH}camera{_id}.frameid", ref frameID, (int)PathfinderType.Int);
			SDKBridgeVolcap.SetValue<SDKMatrix4x4>($"{CAMERAS_PATH}camera{_id}.projectionmatrix", ref camera.pose.projectionMatrix, (int)PathfinderType.Matrix4x4);
			SDKBridgeVolcap.SetValue<SDKVector3>($"{CAMERAS_PATH}camera{_id}.positionlocal", ref camera.pose.localPosition, (int)PathfinderType.Vector3);
			SDKBridgeVolcap.SetValue<SDKQuaternion>($"{CAMERAS_PATH}camera{_id}.rotationlocal", ref camera.pose.localRotation, (int)PathfinderType.Quaternion);
			SDKBridgeVolcap.SetValue<SDKVector3>($"{CAMERAS_PATH}camera{_id}.lossyscale", ref cameraLossyScale, (int)PathfinderType.Vector3);
			SDKBridgeVolcap.SetValue<float>($"{CAMERAS_PATH}camera{_id}.nearclipplane", ref camera.pose.nearClipPlane, (int)PathfinderType.Float);
			SDKBridgeVolcap.SetValue<float>($"{CAMERAS_PATH}camera{_id}.farclipplane", ref camera.pose.farClipPlane, (int)PathfinderType.Float);
			SDKBridgeVolcap.SetValue<float>($"{CAMERAS_PATH}camera{_id}.verticalfov", ref camera.pose.verticalFieldOfView, (int)PathfinderType.Float);
		}
		
		public void Dispose()
		{
			if (_cameraInstance != null)
			{
				Object.Destroy(_cameraInstance.gameObject);
				_cameraInstance = null;
			}
			
			SDKVolumetricUtils.DestroyTexture(ref _colorTexture);

			// SDKVolumetricUtils.DisposeObject(ref _livCaptureDepthCommandBuffer);
			// SDKVolumetricUtils.DisposeObject(ref _livCaptureColorCommandBuffer);
		}
	}
	
	public class SDKVolumetricRenderer : IDisposable
	{
		private VolumetricCapture _volumetricCapture = null;
		private SDKCamera[] _sdkCameras;
		
		public VolumetricCapture volumetricCapture
		{
			get { return _volumetricCapture; }
		}

		protected CameraSetup[] _cameraSetups = null;
		public CameraSetup[] cameraSetups {
			get {
				return _cameraSetups;
			}
		}

		public Camera cameraReference {
			get {
				return _volumetricCapture.cameraPrefab == null ? _volumetricCapture.HMDCamera : _volumetricCapture.cameraPrefab;
			}
		}

		public Transform stage {
			get {
				return _volumetricCapture.stage;
			}
		}

		public string[] excludeBehaviours
		{
			get
			{
				return _volumetricCapture.excludeBehaviours;
			}
		}

		public Matrix4x4 stageLocalToWorldMatrix {
			get {
				return _volumetricCapture.stage == null ? Matrix4x4.identity : _volumetricCapture.stage.localToWorldMatrix;
			}
		}

		public Matrix4x4 localToWorldMatrix {
			get {
				return _volumetricCapture.stageTransform == null ? stageLocalToWorldMatrix : _volumetricCapture.stageTransform.localToWorldMatrix;
			}
		}

		public int spectatorLayerMask {
			get {
				return _volumetricCapture.spectatorLayerMask;
			}
		}

		public SDKVolumetricRenderer(VolumetricCapture volumetricCapture)
		{
			_volumetricCapture = volumetricCapture;
			CreateAssets();
		}

		void CreateAssets()
		{
			SDKVolumetricBridge.Create();
		}

		public void Render()
		{
			SDKVolumetricBridge.GetCameras(ref _sdkCameras);
			SDKVolumetricBridge.SetCameraCount(_sdkCameras.Length);
			
			if (_sdkCameras == null || _sdkCameras.Length == 0)
			{
				DestroyCameras();
			}

			if (_cameraSetups == null || _cameraSetups.Length != _sdkCameras.Length)
			{
				DestroyCameras();
				_cameraSetups = new CameraSetup[_sdkCameras.Length];
				for (int i = 0; i < _cameraSetups.Length; i++)
				{
					_cameraSetups[i] = new CameraSetup(this, i);
				}
			}
			
			InvokePreRender();
			for (int i = 0; i < _cameraSetups.Length; i++)
			{
				_cameraSetups[i].Render(_sdkCameras[i]);
			}
			IvokePostRender();
			
			// Submit to LIV
			GL.IssuePluginEvent(SDKVolumetricBridge.GetRenderEventFunc(), (int)PLUGIN_EVENT.SUBMIT_TO_LIV);
		}

		void DestroyCameras()
		{
			if (_cameraSetups != null)
			{
				for (int i = 0; i < _cameraSetups.Length; i++)
				{
					 if(_cameraSetups[i] != null) _cameraSetups[i].Dispose();
					 _cameraSetups[i] = null;
				}

				_cameraSetups = null;
			}
		}
		
		protected void InvokePreRender()
		{
			if(_volumetricCapture.onPreRender != null) _volumetricCapture.onPreRender.Invoke(this);
		}

		protected void IvokePostRender()
		{
			if(_volumetricCapture.onPostRender != null) _volumetricCapture.onPostRender.Invoke(this);
		}

		public void Dispose()
		{
			DestroyCameras();
			SDKVolumetricBridge.Release();
		}
	}
}