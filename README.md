# MoreQuickSlots

Increases the number of freely assignable quick slots (7 by default: keys 4–0) by up to 6 additional slots.

## How it works

EFT manages quick slots through the `EBoundItem` enum (`Item4`…`Item10` = the 7 free slots). The mod
uses undefined enum values starting at `12` for the extra slots:

- **UI**: A Harmony prefix on `InventoryScreenQuickAccessPanel.Show` clones the last vanilla
  slot (`Item10`) and registers the clones in the panel's `_boundItems` dictionary. Drag & drop
  binding, selection highlight, healing/grenade selectors etc. pick them up automatically.
  Applies to both the inventory panel and the in-raid bar.
- **Hotkey labels**: `ControlSettingsClass.GetBoundItemNames` throws an exception for unknown
  values – a prefix returns the label of the configured hotkey instead.
- **Server compatibility**: The client serializes undefined enum values as JSON numbers, but the
  SPT server expects a string for `Bind`/`Unbind` (`InventoryBindRequestData.Index`).
  A prepended `JsonConverter` therefore always writes `EBoundItem` as a string
  (`"Item4"`, `"12"`, …). The server stores bindings generically in `Inventory.FastPanel`
  (a dictionary with string keys), so **no server mod is required** and bindings persist
  across sessions/raids.
- **Hotkeys**: A MonoBehaviour polls the configured keys (BepInEx `KeyboardShortcut`).
  Just like the vanilla keys 4–0: with an item under the cursor in the inventory, the key
  binds the item. In raid: a short tap uses/equips the bound item (on key release, like
  vanilla); **hold the key + mouse wheel** opens the body part selection (healing items) or
  grenade selection, scrolling selects, **releasing confirms** – identical to the vanilla slots.

## Configuration (F12 / ConfigurationManager)

| Option | Description |
| --- | --- |
| `General / ExtraSlotCount` | Number of additional slots (1–6, default 3). |
| `Hotkeys / ExtraSlot1..6` | Key per extra slot. Default: `-`, `=`, rest unassigned. |

## Build

```powershell
dotnet build MoreQuickSlots.csproj -c Release
```

The DLL is automatically copied to `BepInEx\plugins\maschine-MoreQuickSlots\`.
Expects the usual folder layout (`C:\SPT\Development\MoreQuickSlots` next to
`C:\SPT\EscapeFromTarkov_Data` and `C:\SPT\BepInEx`).

## Uninstalling

> **Important:** Before removing the mod, **empty all extra slots** (drag the items off the
> slots). The bindings live in the server profile (`Inventory.fastPanel`); the vanilla client
> does not know slot numbers `12`+ and the character/inventory screen will no longer load
> (without any error message).

If this has already happened, there are two ways out:

1. Reinstall the mod, empty the extra slots, then uninstall again; **or**
2. Clean the profile manually: stop the server and delete all entries with keys `"12"`
   through `"17"` from the `fastPanel` objects in `SPT\user\profiles\<profileId>.json`
   (appears twice: PMC and Scav character).

## Fika compatibility

Compatible with Fika (verified against Fika 2.3.4):

- Only players who want to use the extra slots need the mod – peers without it are
  unaffected. The extra slot numbers that end up in synced profiles are inert on machines
  without the mod (nothing renders another player's quick slot bindings).
- Dedicated/headless hosts are unaffected: no quick slot UI is shown there and all player
  accesses are null-guarded.
- Binding/unbinding and item use run through the regular per-player SPT server requests
  and Fika hands packets; nothing raid-wide is touched.
- Caveat: should a future Fika version serialize enums by name instead of by raw value,
  profiles containing extra-slot bindings could confuse peers without the mod – worth
  re-checking after major Fika updates.

## Notes / Limitations

- Rebinding by assigning a slot twice, drag & drop onto a slot and removing items by
  dragging them off a slot all work exactly like the vanilla slots.
- When `ExtraSlotCount` is reduced, the surplus slots are removed the next time the
  inventory is opened (or a raid starts) and their bindings are released automatically –
  no orphaned entries remain in the profile.
