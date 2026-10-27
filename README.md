# GVK_TreeNoPhysics

A lightweight client & server companion mod for Space Engineers that disables Havok collision physics for planet environment tree sectors.

Trees remain fully visible — only their collision is removed. Vehicles, players, and grids pass straight through them.

## Philosophy
- **Remove cost without changing the world.** Trees stay visible; only the collision work goes.
- **No settings.** It is on while loaded, so there is nothing to misconfigure.

## Requirements
No other mod is needed. For the full server-side benefit pair it with the PhysicsOptimizations Torch plugin (see below). Loaded with the rest of the server mods ([Steam collection](https://steamcommunity.com/sharedfiles/filedetails/?id=2650582206)).

## Why

On planets like Kharak with dense tree/foliage cover, Space Engineers bakes Havok static collision meshes (`HkStaticCompoundShape`) for every loaded environment sector purely so trees can be solid. That collision baking and the resulting broadphase/narrowphase solver work is a recurring CPU cost for very little gameplay value, and it's the source of constant low-speed vehicle snagging on Kharak's terrain.

This mod removes tree collision entirely, on the client, so nothing about the world visually changes and nothing desyncs from the server.

## Features

- **Zero client desync** — disables the `Physics.Enabled` state on `MyEnvironmentSector` synchronously, on the same 10-tick cadence Keen's own environment system uses, so clients and the server always agree.
- **Zero allocations in the hot path** — no LINQ, no per-frame lists, no spatial queries; only a flat scan of each tracked planet's already-loaded sector children.
- **Whole-sector coverage** — disables collision for an entire 1–2 km sector as soon as it's active, instead of the vanilla distance-based partial cutoffs.
- **Vanilla client compatible** — standard world mod, no client-side plugin required to see the effect (it works stand-alone on any server, modded or not).

## How it works

`GVK_TreeNoPhysicsSession` is a `MySessionComponentBase` that:

1. Tracks every `MyPlanet` currently loaded, via an initial world scan plus `OnEntityAdd` / `OnEntityRemove` hooks.
2. Every 10 ticks (matching `MyPlanetEnvironmentSessionComponent.TIME_TO_UPDATE`), walks each planet's active `MyEnvironmentSector` children.
3. Sets `sector.Physics.Enabled = false` on any sector whose physics is currently on.

Because this runs on every client and the server, no one's simulation ever disagrees about whether a given tree sector has collision — it's always off, everywhere, at the same cadence.

## Companion server plugin

This mod pairs with the **PhysicsOptimizations** Torch plugin's `TreePhysicsOptimizer` module. The plugin goes one step further on the dedicated server: it registers a prefix patch (via Torch's `PatchManager`, not a separate Harmony instance) on `MyEnvironmentSector.EnablePhysics` that forces the physics flag to `false` before Havok ever bakes the collision shape — so the server never pays the construction cost in the first place, rather than disabling physics after the fact.

Running the mod without the plugin is safe (pure client/server-side collision toggle, vanilla-compatible). Running the plugin without the mod is also safe, but is missing the client-authoritative half of the picture on unmodded clients connecting to a server that expects the mod. For the GVK server, both should be active together.

## Installation

1. Subscribe to / add the mod to your Space Engineers world's mod list.
2. (Server) Install and enable the `PhysicsOptimizations` Torch plugin with `DisableTreePhysics` on in its config for the full server-side benefit.
3. No further configuration needed — the mod has no settings; it is always active while loaded.

## Compatibility

- Safe on any planet, vanilla or modded, with or without environment items (trees, bushes, rocks-as-trees, etc.) — it only touches sectors that exist.
- Does not modify tree visuals, spawn rules, or environment item definitions — appearance and distribution are untouched.
- No known conflicts with other mods; it does not patch or replace any vanilla type, only toggles an existing public property.

## Quick links
- Hub board and issues: [GV-Server-Mods/GVK-Settings](https://github.com/GV-Server-Mods/GVK-Settings)
- Workshop: [3807055816](https://steamcommunity.com/sharedfiles/filedetails/?id=3807055816)
- Steam guide: [players' rules and gameplay](https://steamcommunity.com/sharedfiles/filedetails/?id=2781522559); mod collection: [2650582206](https://steamcommunity.com/sharedfiles/filedetails/?id=2650582206)
- Skills: [se-dev-kharak](https://github.com/GV-Server-Mods/se-dev-kharak)

## Credits

Built for the GV: Deserts of Kharak (GVK) server. Published as [Workshop 3807055816](https://steamcommunity.com/sharedfiles/filedetails/?id=3807055816).

## Issues

Tracked in the GVK hub: [GV-Server-Mods/GVK-Settings](https://github.com/GV-Server-Mods/GVK-Settings/issues).

## License

[AGPL-3.0](LICENSE.txt)
