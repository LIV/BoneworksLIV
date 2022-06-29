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
        private string key = "game.toWorld";
        private const string pathBase = "LIV.FPSSTAB.";
        public SDKRigidTransform outputLocal;
        public SDKRigidTransform outputGlobal;
        
        public PathfinderRigidTransformSet(IntPtr ptr) : base(ptr)
		{
		}
        
        private void Update()
        {
            rigidTransform.pos = transform.position;
            rigidTransform.rot = transform.rotation;

            SDKBridgePathfinder.SetValue<SDKRigidTransform>($"{pathBase}{key}", ref rigidTransform, (int) PathfinderType.RigidTransform);
                
            SDKBridgePathfinder.GetValue<SDKRigidTransform>($"{pathBase}{key}", out outputLocal, (int) PathfinderType.RigidTransform);
            SDKBridgePathfinder.GetValue<SDKRigidTransform>($"LIV.FPSSTAB.{key}", out outputGlobal, (int) PathfinderType.RigidTransform);
            
        }
    }
}