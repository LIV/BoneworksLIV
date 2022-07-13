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
    
    public class PathfinderRigidTransform: MonoBehaviour
    {
        private SDKRigidTransform rigidTransform;
        public string Key;
        public string PathBase;
        private string path;
        
        public PathfinderRigidTransform(IntPtr ptr) : base(ptr)
	    {
	    }

        private void Start()
        {
            path = $"{PathBase}.{Key}";
        }

        public void SetPathfinderValuesLocally()
        {
            if (!PathfinderAvatarTrackers.Root) return;
            
            rigidTransform.pos = PathfinderAvatarTrackers.Root.InverseTransformPoint(transform.position);
            rigidTransform.rot = Quaternion.Inverse(PathfinderAvatarTrackers.Root.rotation) * transform.rotation;

            SDKBridgePathfinder.SetValue(path, ref rigidTransform, (int) PathfinderType.RigidTransform);
        }
    }
}