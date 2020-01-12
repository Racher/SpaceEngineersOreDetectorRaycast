using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		readonly IMyOreDetector detector;
		readonly IMyTextSurface surface;
		string name;
		double distance;

		public Program()
		{
			var detectors = new List<IMyOreDetector>();
			GridTerminalSystem.GetBlocksOfType(detectors);
			detector = detectors.First();
			surface = Me.GetSurface(0);
			surface.ContentType = ContentType.TEXT_AND_IMAGE;
			surface.FontSize = 3;
			Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10 | UpdateFrequency.Update1;
			Test();
		}

		void Test()
		{
			var t = new Vector3D(-0.75, 0, 0.5);
			detector.SetValue("RaycastTarget", t);
			if (detector.GetValue<Vector3D>("RaycastTarget") != t)
				throw new Exception("RaycastTarget inequal");
			detector.SetValue("RaycastTarget", detector.GetPosition() + detector.WorldMatrix.Forward);
			var res = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
			if (res.TimeStamp == 0)
				throw new Exception("RaycastResult invalid");
			string blackList = "Stone,Ice";
			detector.SetValue("OreBlacklist", blackList);
			if (detector.GetValue<string>("OreBlacklist") != blackList)
				throw new Exception("OreBlacklist inequal");
			try
			{
				detector.SetValue("ScanEpoch", 0L);
			}
			catch
			{
				return;
			}
			throw new Exception("shouldnt be able to write ScanEpoch");
		}

		public void Main(string argument, UpdateType updateSource)
		{
			var AvailableScanRange = detector.GetValue<double>("AvailableScanRange");
			if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
			{
				detector.SetValue("RaycastTarget", detector.GetPosition() + detector.WorldMatrix.Forward * ((updateSource & UpdateType.Update100) != 0 ? AvailableScanRange / 2 : 100));
				var result = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
				distance = (result.HitPosition - (Vector3D?)detector.GetPosition())?.Length() ?? double.NaN;
				name = result.Name;
			}
			surface.WriteText(name + "\n" + distance.ToString() + "\n" + AvailableScanRange.ToString());
		}
	}
}
