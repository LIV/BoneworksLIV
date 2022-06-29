using System;
using LIV.SDK.Unity;
using UnityEngine;

namespace BoneworksLIV.AvatarTrackers
{
    public struct SDKRigidTransform
    {
        public SDKVector3 pos;
        public SDKQuaternion rot;
    }
    
    public class PathfinderRigidTransformSet: MonoBehaviour
    {
        private SDKRigidTransform rigidTransform;
        public Transform Root;
        public string Key;
        private const string pathBase = "LIV.FPSSTAB.";
        
        public PathfinderRigidTransformSet(IntPtr ptr) : base(ptr)
		{
		}
        
        private void Update()
        {
            if (!Root) return;

            rigidTransform.pos = Root.InverseTransformPoint(transform.position);
            rigidTransform.rot = Quaternion.Inverse(Root.rotation) * transform.rotation;

            SDKBridgePathfinder.SetValue($"{pathBase}{Key}", ref rigidTransform, (int) PathfinderType.RigidTransform);

        }
    }
}