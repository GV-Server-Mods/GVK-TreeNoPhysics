using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.WorldEnvironment;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace GVK.TreeNoPhysics
{
    /// <summary>
    /// GVK Tree No-Physics Companion Mod
    /// Synchronously disables Havok static collision bodies for planet environment tree sectors
    /// on clients and servers, providing high-performance tree phasing with zero client desync.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class GVK_TreeNoPhysicsSession : MySessionComponentBase
    {
        // Keen's MyPlanetEnvironmentSessionComponent updates every 10 ticks (TIME_TO_UPDATE = 10).
        // Synchronizing on this 10-tick cadence catches sectors the instant they are activated.
        private const int UPDATE_INTERVAL_TICKS = 10;

        private int _ticks = 0;
        private readonly List<MyPlanet> _planets = new List<MyPlanet>();
        private readonly HashSet<IMyEntity> _tempEntities = new HashSet<IMyEntity>();

        public override void LoadData()
        {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemoved;
        }

        public override void BeforeStart()
        {
            // Discover all existing planets loaded in the world
            _tempEntities.Clear();
            MyAPIGateway.Entities.GetEntities(_tempEntities, IsPlanetPredicate);
            foreach (var ent in _tempEntities)
            {
                var planet = ent as MyPlanet;
                if (planet != null && !_planets.Contains(planet))
                {
                    _planets.Add(planet);
                }
            }
            _tempEntities.Clear();

            // Run an immediate initial sweep on startup
            SweepActiveSectors();
        }

        private static bool IsPlanetPredicate(IMyEntity entity)
        {
            return entity is MyPlanet;
        }

        private void OnEntityAdded(IMyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null && !_planets.Contains(planet))
            {
                _planets.Add(planet);
            }
        }

        private void OnEntityRemoved(IMyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
            {
                _planets.Remove(planet);
            }
        }

        public override void UpdateAfterSimulation()
        {
            _ticks++;
            if (_ticks % UPDATE_INTERVAL_TICKS != 0)
            {
                return;
            }

            SweepActiveSectors();
        }

        /// <summary>
        /// Inspects the active physics sectors of all registered planets.
        /// Planets typically have only 2 to 6 active sectors at any time.
        /// Zero allocations, no spatial pruning queries, and disables collision across entire sectors.
        /// </summary>
        private void SweepActiveSectors()
        {
            for (int p = _planets.Count - 1; p >= 0; p--)
            {
                var planet = _planets[p];
                if (planet == null || planet.Closed || planet.MarkedForClose)
                {
                    _planets.RemoveAt(p);
                    continue;
                }

                if (planet.Hierarchy == null) continue;

                var children = planet.Hierarchy.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    var sector = children[i].Container.Entity as MyEnvironmentSector;
                    if (sector != null && sector.Physics != null && sector.Physics.Enabled)
                    {
                        sector.Physics.Enabled = false;
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemoved;
            _planets.Clear();
            _tempEntities.Clear();
        }
    }
}
