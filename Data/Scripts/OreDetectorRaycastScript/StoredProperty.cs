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
    class StoredProperty<T>
    {
        readonly Func<T, string> Serialize;
        readonly Func<string, T> Deserialize;
        readonly T invalid;
        readonly string name;
        readonly Guid guid;

        public StoredProperty(string name, bool ingame, Func<T> GetInitial, Func<T, string> Serialize, Func<string, T> Deserialize)
		{
            this.Serialize = Serialize;
            this.Deserialize = Deserialize;
            this.invalid = GetInitial();
            this.name = name;
            guid = Guid.NewGuid();
            try
            {
                OreDetectorRaycast.initers.Add((Entity) =>
                {
                    try
                    {
                        if (Entity.Storage == null)
                            Entity.Storage = new Sandbox.Game.EntityComponents.MyModStorageComponent();
                        Entity.Storage.Add(guid, Serialize(GetInitial()));
                    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in StoredProperty " + name + " initer: " + e.Message);
                    }
                });
                if (ingame)
                {
                    List<IMyTerminalControl> controls;
                    MyAPIGateway.TerminalControls.GetControls<IMyOreDetector>(out controls);
                    if (controls.Any(c => c.Id == name))
                        MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Name collision IMyOreDetector." + name);
                    var prop = MyAPIGateway.TerminalControls.CreateProperty<T, IMyOreDetector>(name);
                    prop.Setter = Set;
                    prop.Getter = Get;
                    prop.Enabled = b => true;
                    prop.Visible = b => false;
                    MyAPIGateway.TerminalControls.AddControl<IMyOreDetector>(prop);
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in StoredProperty.StoredProperty " + name + ": " + e.Message);
            }
        }

        public T Get(IMyEntity Entity)
        {
            if (Entity == null || Entity.Storage == null)
                return invalid;
            try
            {
                return Deserialize(Entity.Storage.GetValue(guid));
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in StoredProperty.Get " + name + ": " + e.Message);
                return invalid;
            }
        }

        public void Set(IMyEntity Entity, T value)
        {
            if (Entity == null || Entity.Storage == null)
                return;
            try
            {
                Entity.Storage.SetValue(guid, Serialize(value));
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("OreDetectorRaycast", "Exception in StoredProperty.Set " + name + ": " + e.Message);
            }
        }
    }
}
