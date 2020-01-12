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
	class ComputedProperty<T>
    {
        public readonly Func<IMyEntity, T> Get;
        public ComputedProperty(string name, Func<IMyEntity, T> Get)
        {
            this.Get = Get;
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyOreDetector>(out controls);
                if (controls.Any(c => c.Id == name))
                    MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Name collision IMyOreDetector." + name);
                var prop = MyAPIGateway.TerminalControls.CreateProperty<T, IMyOreDetector>(name);
                prop.Getter = Get;
                prop.Enabled = b => true;
                prop.Visible = b => false;
                MyAPIGateway.TerminalControls.AddControl<IMyOreDetector>(prop);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in ComputedProperty.ComputedProperty " + name + ": " + e.Message);
            }
        }
    }
}
