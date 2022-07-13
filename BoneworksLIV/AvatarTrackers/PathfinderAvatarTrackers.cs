using System;
using System.Collections.Generic;
using StressLevelZero.VRMK;
using UnityEngine;

namespace BoneworksLIV.AvatarTrackers
{
    public class PathfinderAvatarTrackers: MonoBehaviour
    {
	    public static Transform Root { get; private set; }
	    
	    private const string localPathBase = "localAvatarTrackers";
	    private const string globalPathBase = "LIV.avatarTrackers";
		private PathfinderRigidTransform toGround;
		private const string toGroundPath = "bob.stage.toGround";
		private readonly List<PathfinderRigidTransform> pathfinderRigidTransforms = new List<PathfinderRigidTransform>();
		private PhysGrounder physGrounder;
		private static readonly Dictionary<string, string> boneMap = new Dictionary<string, string>()
		{
			{ "Head_JawSHJnt", "bob.stage.avatar.trackers.head" }, // other options: Neck_01SHJnt / Neck_02SHJnt / Neck_TopSHJnt / Head_JawSHJnt / Head_TopSHJnt
			// { "Spine_TopSHJnt", "bob.stage.avatar.trackers.chest" }, // TODO: chest looks hella broken, better off not tracking it at all.
			{ "ROOTSHJnt", "bob.stage.avatar.trackers.waist" },
			{ "l_Hand_1SHJnt", "bob.stage.avatar.trackers.leftHand" }, // other options:  l_Hand_1SHJnt / l_Hand_2SHJnt / l_GripPoint_AuxSHJnt
			{ "l_Arm_Elbow_CurveSHJnt", "bob.stage.avatar.trackers.leftElbowGoal" },
			{ "r_Hand_1SHJnt", "bob.stage.avatar.trackers.rightHand" }, // other options: r_Hand_1SHJnt / r_Hand_2SHJnt / r_GripPoint_AuxSHJnt
			{ "r_Arm_Elbow_CurveSHJnt", "bob.stage.avatar.trackers.rightElbowGoal" },
			{ "l_Leg_AnkleSHJnt", "bob.stage.avatar.trackers.leftFoot" }, // other options:  l_Leg_BallSHJnt
			{ "l_Leg_KneeSHJnt", "bob.stage.avatar.trackers.leftKneeGoal" },
			{ "r_Leg_AnkleSHJnt", "bob.stage.avatar.trackers.rightFoot" }, // other options: r_Leg_BallSHJnt
			{ "r_Leg_KneeSHJnt", "bob.stage.avatar.trackers.rightKneeGoal" },
		};

		public PathfinderAvatarTrackers(IntPtr ptr) : base(ptr)
		{
		}

        private void Awake()
        {
	        var skeleton = transform.Find("SHJntGrp");
			var children = skeleton.GetComponentsInChildren<Transform>();
			Root = BoneworksLivMod.Stage;
			foreach (var child in children)
			{
				if (boneMap.ContainsKey(child.name))
				{
					pathfinderRigidTransforms.Add(CreatePathfinderTransform(child, boneMap[child.name]));
				}
			}

			toGround = CreatePathfinderTransform(skeleton, toGroundPath);
        }

        private void Start()
        {
	        // TODO use a better way to find this component.
	        physGrounder = FindObjectOfType<PhysGrounder>();
        }

        private PathfinderRigidTransform CreatePathfinderTransform(Transform child, string path)
        {
	        var pathfinderTransform = new GameObject($"Pathfinder-{child.name}").AddComponent<PathfinderRigidTransform>();
			pathfinderTransform.transform.SetParent(transform, false);
			pathfinderTransform.Key = path;
			pathfinderTransform.PathBase = localPathBase;

			if (child.name.StartsWith("r_"))
			{
				pathfinderTransform.transform.localEulerAngles = new Vector3(0f, -90f, 90f);
			} else if (child.name.StartsWith("l_"))
			{
				pathfinderTransform.transform.localEulerAngles = new Vector3(0f, 90f, 90f);
			} else if (child.name == "ROOTSHJnt")
			{
				pathfinderTransform.transform.localEulerAngles = new Vector3(90f, -90f, 0);
			}

			return pathfinderTransform;
        }
        
        private void Update()
        {
	        foreach (var pathfinderRigidTransform in pathfinderRigidTransforms)
	        {
		        pathfinderRigidTransform.SetPathfinderValuesLocally();
	        }

	        // TODO clear the local tree before setting stuff that needs to be unset.
	        var grounded = physGrounder.isGrounded;
	        if (grounded)
	        {
				toGround.SetPathfinderValuesLocally();
	        }
	        // TODO I don't think this is working.
	        SDKBridgePathfinder.SetValue($"{localPathBase}.bob.stage.isGrounded", ref grounded, (int) PathfinderType.Boolean);

	        SDKBridgePathfinder.CopyPath(globalPathBase, localPathBase);
        }
    }
}