using System;
using System.Reflection;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoreQuickSlots
{
    internal class QuickSlotHotkeyHandler : MonoBehaviour
    {
        private static readonly FieldInfo HealingSelectorField =
            AccessTools.Field(typeof(InventoryScreenQuickAccessPanel), "healingSelector");
        private static readonly FieldInfo GrenadeSelectorField =
            AccessTools.Field(typeof(InventoryScreenQuickAccessPanel), "grenadeSelector");
        private static InventoryScreenQuickAccessPanel _battlePanel;

        private readonly bool[] _keyHeld = new bool[Plugin.MaxExtraSlots];
        private readonly bool[] _keyConsumedByBind = new bool[Plugin.MaxExtraSlots];

        internal static void RegisterBattlePanel(InventoryScreenQuickAccessPanel panel)
        {
            _battlePanel = panel;
        }

        private void Update()
        {
            if (IsTypingIntoInputField())
            {
                return;
            }

            InventoryScreenQuickAccessPanel panel =
                (_battlePanel != null && _battlePanel.isActiveAndEnabled) ? _battlePanel : null;
            var healing = panel != null ? (HealingLimbSelector)HealingSelectorField.GetValue(panel) : null;
            var grenades = panel != null ? (GrenadeSelector)GrenadeSelectorField.GetValue(panel) : null;

            for (int i = 0; i < Plugin.ExtraSlotCount.Value; i++)
            {
                var key = Plugin.SlotKeys[i].Value;
                var index = (EBoundItem)(Plugin.FirstExtendedValue + i);

                if (IsShortcutDown(key))
                {
                    _keyHeld[i] = true;
                    _keyConsumedByBind[i] = TryBindHoveredItem(index);
                }
                else if (_keyHeld[i] && !Input.GetKey(key.MainKey) && !Input.GetKeyUp(key.MainKey))
                {
                    _keyHeld[i] = false;
                    _keyConsumedByBind[i] = false;
                }

                if (!_keyHeld[i])
                {
                    continue;
                }

                if (_keyConsumedByBind[i])
                {
                    if (Input.GetKeyUp(key.MainKey))
                    {
                        _keyHeld[i] = false;
                        _keyConsumedByBind[i] = false;
                    }
                    continue;
                }

                bool healingActive = healing != null && healing.IsActive;
                bool grenadesShown = grenades != null && grenades.IsShown;
                if (IsShortcutHeld(key) && !healingActive && !grenadesShown &&
                    Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
                {
                    TryOpenSelector(panel, index);
                    continue;
                }

                if (!Input.GetKeyUp(key.MainKey))
                {
                    continue;
                }
                _keyHeld[i] = false;
                if (healingActive)
                {
                    ConfirmSelection(healing);
                }
                else if (grenadesShown)
                {
                    ConfirmSelection(grenades);
                }
                else
                {
                    TryUseQuickSlot(index);
                }
            }
        }
        private static bool IsShortcutDown(KeyboardShortcut key)
        {
            return key.MainKey != KeyCode.None && Input.GetKeyDown(key.MainKey) && ModifiersHeld(key);
        }

        private static bool IsShortcutHeld(KeyboardShortcut key)
        {
            return key.MainKey != KeyCode.None && Input.GetKey(key.MainKey) && ModifiersHeld(key);
        }

        private static bool ModifiersHeld(KeyboardShortcut key)
        {
            foreach (KeyCode modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }
        private static bool IsTypingIntoInputField()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null)
            {
                return false;
            }

            TMP_InputField tmpField = selected.GetComponent<TMP_InputField>();
            if (tmpField != null && tmpField.isFocused)
            {
                return true;
            }

            InputField legacyField = selected.GetComponent<InputField>();
            return legacyField != null && legacyField.isFocused;
        }
        private static bool TryBindHoveredItem(EBoundItem index)
        {
            try
            {
                ItemUiContext context = ItemUiContext.Instance;
                if (context == null || !context.isActiveAndEnabled || context.CurrentItemContext == null)
                {
                    return false;
                }

                Item item = context.CurrentItemContext.Item;
                if (item == null)
                {
                    return false;
                }
                context.BindItem(item, index);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Binding hovered item failed: {ex}");
                return false;
            }
        }
        private static void TryUseQuickSlot(EBoundItem index)
        {
            try
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player == null || !player.IsYourPlayer || player.HandsController == null ||
                    player.HealthController == null || !player.HealthController.IsAlive)
                {
                    return;
                }

                if (player.HandsController.InCanNotBeInterruptedOperation())
                {
                    return;
                }

                player.SetQuickSlotItem(index, _ => { });
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Using quick slot failed: {ex}");
            }
        }
        private static void TryOpenSelector(InventoryScreenQuickAccessPanel panel, EBoundItem index)
        {
            try
            {
                if (panel == null || !Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                if (player == null || !player.IsYourPlayer)
                {
                    return;
                }

                Item item = player.InventoryController.Inventory.FastAccess.GetBoundItem(index);
                if (item != null)
                {
                    panel.TryShowSelectors(index, item);
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Opening quick slot selector failed: {ex}");
            }
        }
        private static void ConfirmSelection(HealingLimbSelector selector)
        {
            try
            {
                selector.SetResult(selector.GetSelectedLimb());
                selector.Close();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Confirming limb selection failed: {ex}");
            }
        }
        private static void ConfirmSelection(GrenadeSelector selector)
        {
            try
            {
                selector.SetResult(selector.GetGrenade());
                selector.Close();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Confirming grenade selection failed: {ex}");
            }
        }
    }
}
