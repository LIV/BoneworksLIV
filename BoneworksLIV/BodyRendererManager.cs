using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoneworksLIV
{
	public class BodyRendererManager : MonoBehaviour
	{
		public List<Renderer> headRenderers = new List<Renderer>();
		private SkinnedMeshRenderer renderer;

		public BodyRendererManager(IntPtr ptr) : base(ptr)
		{
		}

		private void Awake()
		{
			renderer = GetComponent<SkinnedMeshRenderer>();
		}

		private void Update()
		{
			foreach (var headRenderer in headRenderers)
			{
				headRenderer.enabled = renderer.enabled;
			}
		}
	}
}