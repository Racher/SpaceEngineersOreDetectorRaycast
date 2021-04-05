using System; 
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage;
using VRageMath;
using Sandbox.ModAPI.Interfaces.Terminal;
using IMyOreDetector = Sandbox.ModAPI.Ingame.IMyOreDetector;

namespace OreDetectorRaycastScript
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false)]
    public class OreDetectorRaycast : MyGameLogicComponent
    {
        const double RaycastTimeMultiplier = 2.0 / 10000; //Meter per Tick
        const double AvailableScanRangeMax = 100000.0; //Meter
        const string DefaultOreBlacklist = "Stone";
        const double VoxelStepConst = 1.0; // Step size constant part.
        const double VoxelStepBase = 1.01; // Step size exponential base.

        public static readonly List<Action<IMyEntity>> initers = new List<Action<IMyEntity>>();
        static readonly HashSet<MyVoxelMaterialDefinition> materialBlacklist = new HashSet<MyVoxelMaterialDefinition>();
        static readonly Random stepOffsetRandom = new Random(0);
        static readonly List<MyVoxelBase> mVoxels = new List<MyVoxelBase>();
        static readonly List<MyLineSegmentOverlapResult<MyVoxelBase>> mVoxelMapOverlaps = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();

        static readonly StoredProperty<long> ScanEpochTick = new StoredProperty<long>(
			"ScanEpoch",
			false,
			i => i.ToString(),
			Convert.ToInt64);

        static readonly StoredProperty<Vector3D> RaycastTarget = new StoredProperty<Vector3D>(
			"RaycastTarget",
			true,
			v => v.ToString(),
			s =>
			{
				Vector3D v;
				Vector3D.TryParse(s, out v);
				return v;
			});

        static readonly StoredProperty<string> OreBlacklist = new StoredProperty<string>(
			"OreBlacklist",
			true,
			s => s,
			s => s);

        static readonly ComputedProperty<double> AvailableScanRange = new ComputedProperty<double>(
            "AvailableScanRange", (Entity) =>
            Math.Min(AvailableScanRangeMax, (MyAPIGateway.Session.GameDateTime.Ticks - ScanEpochTick.Get(Entity)) * RaycastTimeMultiplier));

        static readonly ComputedProperty<MyDetectedEntityInfo> DirectResult = new ComputedProperty<MyDetectedEntityInfo>(
            "DirectResult", (Entity) =>
        {
            if (Entity == null)
                return new MyDetectedEntityInfo();
            try {
                var AvailableScanRange = OreDetectorRaycast.AvailableScanRange.Get(Entity);
                var OreBlacklist = OreDetectorRaycast.OreBlacklist.Get(Entity);
                var RaycastTarget = OreDetectorRaycast.RaycastTarget.Get(Entity);
                var detector = Entity as IMyOreDetector;

                var disp = detector.GetPosition() - RaycastTarget;
                var length = disp.Length();
                if (AvailableScanRange < length)
                    return new MyDetectedEntityInfo();
                mVoxels.Clear();
                var sphere = new BoundingSphereD(RaycastTarget, 0.1);
                MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, mVoxels);
                
                MyVoxelBase hit = null;
                MyVoxelMaterialDefinition material = null;
                foreach (var vox in mVoxels) {
                    material = vox.GetMaterialAt(ref RaycastTarget);
                    if (material != null) {
                        hit = vox;
                    }
                }

                var currentChargeTicks = (long)((AvailableScanRange - length) / RaycastTimeMultiplier);
                ScanEpochTick.Set(Entity, MyAPIGateway.Session.GameDateTime.Ticks - currentChargeTicks);
                if (hit == null)
                    return new MyDetectedEntityInfo(0, "", MyDetectedEntityType.None,
                        null, MatrixD.Identity, Vector3.Zero,
                        MyRelationsBetweenPlayerAndBlock.NoOwnership, BoundingBox.CreateInvalid(), MyAPIGateway.Session.GameDateTime.Ticks);

                
                var ore = material.MinedOre;
                return new MyDetectedEntityInfo(1, ore, MyDetectedEntityType.Unknown,
                    RaycastTarget, MatrixD.Identity, Vector3.Zero,
                    MyRelationsBetweenPlayerAndBlock.NoOwnership, BoundingBox.CreateInvalid(), MyAPIGateway.Session.GameDateTime.Ticks);
            } catch (Exception e) {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in GetDirectResult: " + e.Message);
                return new MyDetectedEntityInfo();
            }
        });
        
        static readonly ComputedProperty<MyDetectedEntityInfo> RaycastResult = new ComputedProperty<MyDetectedEntityInfo>(
            "RaycastResult", (Entity) =>
        {
            if (Entity == null)
                return new MyDetectedEntityInfo();
            try
            {
                var AvailableScanRange = OreDetectorRaycast.AvailableScanRange.Get(Entity);
                var OreBlacklist = OreDetectorRaycast.OreBlacklist.Get(Entity);
                var RaycastTarget = OreDetectorRaycast.RaycastTarget.Get(Entity);
                var detector = Entity as IMyOreDetector;

                materialBlacklist.Clear();
                if (OreBlacklist != null)
                    foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                        if (OreBlacklist.Contains(material.MinedOre))
                            materialBlacklist.Add(material);

                
                var ray = new LineD(detector.GetPosition(), RaycastTarget);
                ray.Length = Math.Min(AvailableScanRange, ray.Length);
                ray.To = ray.From + ray.Direction * ray.Length;
                mVoxelMapOverlaps.Clear();
                MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref ray, mVoxelMapOverlaps);

                MyVoxelBase hit = null;
                var maxDist = ray.Length;
                foreach (var voxelMapOverlap in mVoxelMapOverlaps)
                {
                    var vMaxDist = Math.Min(maxDist, voxelMapOverlap.Distance + voxelMapOverlap.Element.Size.Length());
                    var dist0 = voxelMapOverlap.Distance + VoxelStepConst * stepOffsetRandom.NextDouble();
                    for (var dist = dist0; dist < vMaxDist; dist *= VoxelStepBase, dist += VoxelStepConst)
                    {
                        var testPosition = detector.GetPosition() + dist * ray.Direction;
                        var material = voxelMapOverlap.Element.GetMaterialAt(ref testPosition);
                        if (material != null && !materialBlacklist.Contains(material))
                        {
                            hit = voxelMapOverlap.Element;
                            maxDist = dist;
                            break;
                        }
                    }
                    if (hit != null) {
                        break;
                    }
                }

                var currentChargeTicks = (long)((AvailableScanRange - ray.Length) / RaycastTimeMultiplier);
                ScanEpochTick.Set(Entity, MyAPIGateway.Session.GameDateTime.Ticks - currentChargeTicks);
                if (hit == null)
                    return new MyDetectedEntityInfo(0, "", MyDetectedEntityType.None,
                        null, MatrixD.Identity, Vector3.Zero,
                        MyRelationsBetweenPlayerAndBlock.NoOwnership, BoundingBox.CreateInvalid(), MyAPIGateway.Session.GameDateTime.Ticks);

                var hitPos = detector.GetPosition() + maxDist * ray.Direction;
                var ore = hit.GetMaterialAt(ref hitPos).MinedOre;
                return new MyDetectedEntityInfo(1, ore, MyDetectedEntityType.Unknown,
                    hitPos, MatrixD.Identity, Vector3.Zero,
                    MyRelationsBetweenPlayerAndBlock.NoOwnership, BoundingBox.CreateInvalid(), MyAPIGateway.Session.GameDateTime.Ticks);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in GetRaycastResult: " + e.Message);
                return new MyDetectedEntityInfo();
            }
        });

        MyObjectBuilder_EntityBase objectBuilder = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            this.objectBuilder = objectBuilder;
            foreach (var initer in initers)
                initer(Entity);
            ScanEpochTick.Set(Entity, MyAPIGateway.Session.GameDateTime.Ticks);
            OreBlacklist.Set(Entity, DefaultOreBlacklist);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return objectBuilder;
        }
    }
}
