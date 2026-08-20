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
- **Hotkeys**: A MonoBehaviour polls the configured keys. It checks the main key and its
  modifiers directly rather than calling BepInEx's `KeyboardShortcut.IsDown()`, which only
  fires while no other key is held – so the slots also work while moving. Polling is
  suppressed while a text field has focus, so typing in a search box or chat never triggers
  a slot. Just like the vanilla keys 4–0: with an item under the cursor in the inventory,
  the key binds the item. In raid: a short tap uses/equips the bound item (on key release,
  like vanilla); **hold the key + mouse wheel** opens the body part selection (healing items)
  or grenade selection, scrolling selects, **releasing confirms** – identical to the vanilla
  slots.

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
All references are relative, so the project folder is expected two levels below the SPT
install root – next to `<SPT>\EscapeFromTarkov_Data` and `<SPT>\BepInEx`.

## Uninstalling

> **Important:** Before removing the mod, **empty all extra slots** (drag the items off the
> slots). The bindings live in the server profile (`Inventory.fastPanel`); the vanilla client
> does not know slot numbers `12`+ and the character/inventory screen will no longer load
> (without any error message).

If this has already happened, there are two ways out:

1. Reinstall the mod, empty the extra slots, then uninstall again; **or**
2. Clean the profile manually: stop the server and delete all entries with keys `"12"`
   through `"17"` from the `fastPanel` objects in
   `SPT_Runtime\user\profiles\<profileId>.json` (appears twice: PMC and Scav character).

## Fika compatibility

Compatible with Fika (checked against the Fika 2.4.0 sources):

- Only players who want the extra slots need the mod – peers without it are unaffected.
  The extra slot numbers do travel with the synced profile, but Fika writes enum values as
  raw binary of fixed width, and on the receiving side they only ever land in plain
  dictionaries. No UI renders another player's quick slot bindings, and looting the corpse
  of a player who uses the mod goes through a controller that never reads them.
- Dedicated/headless hosts are unaffected: no quick slot UI exists there and all player
  accesses are null-guarded.
- Binding/unbinding and item use run through the regular per-player SPT server requests and
  Fika's normal replication; nothing raid-wide is touched.
- Typing in Fika's in-raid chat does not trigger the slots – the text-field guard covers it.
- Known limitation: Fika's host-only raid admin UI (opened with the `openAdminUI` console
  command) does not suppress the hotkeys. Pressing a slot key while it is open uses the
  bound item in the background.
- Caveat: the extra slots use enum values the game itself does not define. They stay
  harmless as long as nobody renders another player's inventory; if a future Fika version
  or another mod adds a spectator or foreign-inventory view, peers without this mod could
  throw on those values. Worth re-checking after major Fika updates.

## Notes / Limitations

- Rebinding by assigning a slot twice, drag & drop onto a slot and removing items by
  dragging them off a slot all work exactly like the vanilla slots.
- When `ExtraSlotCount` is reduced, the surplus slots are removed the next time the
  inventory is opened (or a raid starts) and their bindings are released automatically –
  no orphaned entries remain in the profile.
