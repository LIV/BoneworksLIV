using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoneworksLIV
{
    public class BoneText: MonoBehaviour
    {
	    private TextMesh textMesh;
	    private Transform mainCamera;
	    
	    public BoneText(IntPtr ptr) : base(ptr)
		{
		}
	    
        private void Awake()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "boneSphere";
			Destroy(sphere.GetComponent<Collider>());
			sphere.transform.SetParent(transform, false);
			sphere.transform.localScale = Vector3.one * 0.005f;
			
			var capsuleX = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			capsuleX.name = "boneCapsuleX";
			Destroy(capsuleX.GetComponent<Collider>());
			capsuleX.transform.SetParent(sphere.transform, false);
			capsuleX.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
			capsuleX.transform.localEulerAngles = Vector3.forward * 90f;
			capsuleX.transform.localPosition = Vector3.right * 2.5f;
			capsuleX.GetComponent<Renderer>().material.color = Color.red;
			
			var capsuleY = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			capsuleY.name = "boneCapsuleY";
			Destroy(capsuleY.GetComponent<Collider>());
			capsuleY.transform.SetParent(sphere.transform, false);
			capsuleY.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
			capsuleY.transform.localPosition = Vector3.up * 2.5f;
			capsuleY.GetComponent<Renderer>().material.color = Color.green;
			
			var capsuleZ = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			capsuleZ.name = "boneCapsuleZ";
			Destroy(capsuleZ.GetComponent<Collider>());
			capsuleZ.transform.SetParent(sphere.transform, false);
			capsuleZ.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
			capsuleZ.transform.localEulerAngles = Vector3.right * 90f;
			capsuleZ.transform.localPosition = Vector3.forward * 2.5f;
			capsuleZ.GetComponent<Renderer>().material.color = Color.blue;
			
			var text = new GameObject("BoneText");
			text.transform.SetParent(transform, false);
			textMesh = text.AddComponent<TextMesh>();
			textMesh.text = transform.name;
			textMesh.transform.localScale = new Vector3(-0.005f, 0.005f, 0.005f);
			mainCamera = Camera.main.transform;
        }

        private void Update()
        {
	        textMesh.transform.LookAt(Camera.main.transform, Camera.main.transform.up);
        }
    }
}