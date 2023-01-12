using System;
using System.Collections;
using System.Collections.Generic;
using LIVPathfinderSDK;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LIV.SDK.Unity.Volumetric.GameSDK
{
	public class CameraSetup : IDisposable
	{
		private int _cameraIndex;
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
		private Vector3 _lossyScale = Vector3.zero;
		public Vector3 lossyScale => _lossyScale;
		
		public CameraSetup(SDKVolumetricRenderer renderer, int cameraIndex)
		{
			_cameraIndex = cameraIndex;
			_renderer = renderer;
			var cloneGO = Object.Instantiate(_renderer.cameraReference.gameObject, _renderer.stage);
			_cameraInstance = cloneGO.GetComponent<Camera>();
			SDKVolumetricUtils.CleanCameraBehaviours(_cameraInstance, _renderer.excludeBehaviours);

			_cameraInstance.name = "Volumetric Camera "+_cameraIndex;
			if (_cameraInstance.CompareTag("MainCamera"))
			{
				_cameraInstance.tag = "Untagged";
			}

			_cameraInstance.transform.localScale = Vector3.one;
			_cameraInstance.rect = new Rect(0, 0, 1, 1);
			_cameraInstance.depth = 0;
#if UNITY_5_4_OR_NEWER
			_cameraInstance.stereoTargetEye = StereoTargetEyeMask.None;
#endif
#if UNITY_5_6_OR_NEWER
            _cameraInstance.allowMSAA = false;
#endif
			int encodedCameraIndex = _cameraIndex * 10;
			_livCaptureDepthCommandBuffer = new CommandBuffer();
			_livCaptureDepthCommandBuffer.name = "LIV.CaptureDepth"+_cameraIndex;
			_livCaptureDepthCommandBuffer.IssuePluginEvent(SDKVolumetricBridge.GetRenderEventFunc(), encodedCameraIndex + (int)PLUGIN_EVENT.CAPTURE_DEPTH);
			
			_livCaptureColorCommandBuffer = new CommandBuffer();
			_livCaptureColorCommandBuffer.name = "LIV.CaptureColor"+_cameraIndex;
			_livCaptureColorCommandBuffer.IssuePluginEvent(SDKVolumetricBridge.GetRenderEventFunc(), encodedCameraIndex + (int)PLUGIN_EVENT.CAPTURE_COLOR);
			
			_cameraInstance.enabled = false;
			_cameraInstance.gameObject.SetActive(true);
		}

		private void UpdateTextures(int width, int height)
		{
			if (
				_colorTexture == null ||
				_colorTexture.width != width ||
				_colorTexture.height != height
			)
			{
				if (SDKVolumetricUtils.CreateTexture(ref _colorTexture, width, height, 24, RenderTextureFormat.ARGB32))
				{
#if UNITY_EDITOR
					_colorTexture.name = "LIV.VolumetricColorRenderTexture"+_cameraIndex;
#endif
				}
				else
				{
					Debug.LogError("LIV: Unable to create volumetric color texture! "+_cameraIndex);
				}
			}
		}
		
		public void Render(RequestedPose pose)
		{
			if (pose.width < 1 ||
			    pose.height < 1)
			{
				Debug.LogError($"LIV: Camera: {_cameraIndex} resolution: {pose.width}x{pose.height} is invalid. ");
				return;
			}
			
			UpdateTextures(pose.width, pose.height);
			if (_colorTexture != null)
			{
				_captureDepthEvent = SDKVolumetricUtils.GetCameraEvent(_cameraInstance);
				_cameraInstance.targetTexture = _colorTexture;
				SDKVolumetricUtils.SetCamera(_cameraInstance, _cameraInstance.transform, pose, _renderer.localToWorldMatrix, _renderer.spectatorLayerMask);
				_cameraInstance.AddCommandBuffer(_captureDepthEvent, _livCaptureDepthCommandBuffer);
				_cameraInstance.AddCommandBuffer(_captureColorEvent, _livCaptureColorCommandBuffer);
				_cameraInstance.Render();
				_cameraInstance.RemoveCommandBuffer(_captureColorEvent, _livCaptureColorCommandBuffer);
				_cameraInstance.RemoveCommandBuffer(_captureDepthEvent, _livCaptureDepthCommandBuffer);
			}

			_lossyScale = _cameraInstance.transform.lossyScale;
		}
		
		public void Dispose()
		{
			if (_cameraInstance != null)
			{
				Object.Destroy(_cameraInstance.gameObject);
				_cameraInstance = null;
			}
			
			SDKVolumetricUtils.DestroyTexture(ref _colorTexture);

			SDKVolumetricUtils.DisposeObject(ref _livCaptureDepthCommandBuffer);
			SDKVolumetricUtils.DisposeObject(ref _livCaptureColorCommandBuffer);
		}
	}
	
	public class SDKVolumetricRenderer : IDisposable
	{
		private VolumetricGameSDK _volumetricGameSDK = null;
		private RequestedFrame _requestedFrame = new RequestedFrame();
		
		public VolumetricGameSDK volumetricGameSDK
		{
			get { return _volumetricGameSDK; }
		}

		protected CameraSetup[] _cameraSetups = null;
		public CameraSetup[] cameraSetups {
			get {
				return _cameraSetups;
			}
		}

		public Camera cameraReference {
			get {
				return _volumetricGameSDK.cameraPrefab == null ? _volumetricGameSDK.HMDCamera : _volumetricGameSDK.cameraPrefab;
			}
		}

		public Transform stage {
			get {
				return _volumetricGameSDK.stage;
			}
		}

		public string[] excludeBehaviours
		{
			get
			{
				return _volumetricGameSDK.excludeBehaviours;
			}
		}

		public Matrix4x4 stageLocalToWorldMatrix {
			get {
				return _volumetricGameSDK.stage == null ? Matrix4x4.identity : _volumetricGameSDK.stage.localToWorldMatrix;
			}
		}

		public Matrix4x4 localToWorldMatrix {
			get {
				return _volumetricGameSDK.stageTransform == null ? stageLocalToWorldMatrix : _volumetricGameSDK.stageTransform.localToWorldMatrix;
			}
		}

		public int spectatorLayerMask {
			get {
				return _volumetricGameSDK.spectatorLayerMask;
			}
		}

		public SDKVolumetricRenderer(VolumetricGameSDK volumetricGameSDK)
		{
			_volumetricGameSDK = volumetricGameSDK;
			SDKVolumetricBridge.set_path(SDKVolumetricBridge.LIV_VOLCAP_GAME_PATH);
			SDKVolumetricBridge.create();
		}

		public void Render()
		{
			SDKVolumetricBridge.GetRequestedFrame(ref _requestedFrame);
			
			if (_requestedFrame.cameraCount <= 0)
			{
				DestroyCameras();
			}

			if (_cameraSetups == null || _cameraSetups.Length != _requestedFrame.cameraCount)
			{
				DestroyCameras();
				_cameraSetups = new CameraSetup[_requestedFrame.cameraCount];
				for (int i = 0; i < _cameraSetups.Length; i++)
				{
					_cameraSetups[i] = new CameraSetup(this, i);
				}
			}
			
			InvokePreRender();
			for (int i = 0; i < _cameraSetups.Length; i++)
			{
				_cameraSetups[i].Render(_requestedFrame.cameras[i]);
			}
			
			SDKVolumetricBridge.SetCameras(ref _requestedFrame, _cameraSetups);
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
			if(_volumetricGameSDK.onPreRender != null) _volumetricGameSDK.onPreRender.Invoke(this);
		}

		protected void IvokePostRender()
		{
			if(_volumetricGameSDK.onPostRender != null) _volumetricGameSDK.onPostRender.Invoke(this);
		}

		public void Dispose()
		{
			DestroyCameras();
			SDKVolumetricBridge.release();
		}
	}
}