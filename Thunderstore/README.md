# No Void Fiend Healing Drone Target

A small Risk of Rain 2 BepInEx mod that prevents healing drones from selecting Void Fiend as a healing target.

Supported drones:

- Healing Drone (`Drone2Body`), including compatible body-name variants.
- Emergency Drone (`EmergencyDroneBody`).
- Operator's DOC (`DTHealingDroneBody`).

The restriction applies whether Void Fiend owns the drone or belongs to an allied player. 
Emergency Drone's additional simultaneous beams also ignore Void Fiend while remaining free to heal other allies.

## Installation

Install the mod with a Thunderstore-compatible mod manager. The required runtime
dependencies are:

- `bbepis-BepInExPack-5.4.2122`
- `RiskofThunder-HookGenPatcher-1.2.9`

All players should use the same mod version during multiplayer testing.

## Building

From the project directory:

```powershell
dotnet build -c Debug
```

The normal build output is written to:

```text
bin/Debug/netstandard2.1/NoVoidFiendHealingDroneTarget.dll
```

After every successful build, the project also refreshes the local Thunderstore package files under `Thunderstore/`, including the DLL in `Thunderstore/plugins/`.

## Testing

1. Create a clean Risk of Rain 2 profile in r2modman or the Thunderstore app.
2. Install BepInExPack, HookGenPatcher, and this local package.
3. Test Healing Drone, Emergency Drone, and DOC with an injured Void Fiend.
4. Confirm that none of them selects Void Fiend.
5. Add an injured non-Void-Fiend ally and confirm that the drones still heal it.
6. For Emergency Drone, confirm that every additional beam also ignores Void
   Fiend.
7. Repeat with another player owning the drone.

If the BepInEx log contains `Could not patch BaseAI.FindEnemyHurtBox`, a game update probably changed that method and the IL hook must be reviewed.

## How it works

- An IL hook removes Void Fiend from the ally search used by healing drone AI.
- An AI evaluation hook rejects remaining direct target selections, including owner targeting paths.
- Emergency Drone's extra-beam predicate excludes Void Fiend from every additional target search.
- Other healing sources are not modified.

## Compatibility

The mod deliberately identifies supported drones by their body-name prefixes.
Mods that replace these AI paths or use unrelated custom drone body names may require explicit compatibility support.

## License

MIT. See `LICENSE`.
