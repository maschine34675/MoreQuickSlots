using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.Settings.Control;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Insurance;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreQuickSlots
{
    [HarmonyPatch(typeof(InventoryScreenQuickAccessPanel), nameof(InventoryScreenQuickAccessPanel.Show),
        typeof(InventoryController), typeof(ItemUiContext), typeof(GamePlayerOwner), typeof(InsuranceCompany))]
    internal static class QuickAccessPanelShowPatch
    {
        private static readonly FieldInfo BoundItemsField =
            AccessTools.Field(typeof(InventoryScreenQuickAccessPanel), "_boundItems");
        private static readonly FieldInfo InstallPlaceField =
            AccessTools.Field(typeof(QuickSlotView), "InstallPlace");
        private static readonly FieldInfo CaptionField =
            AccessTools.Field(typeof(QuickSlotView), "Caption");

        [HarmonyPrefix]
        private static void Prefix(InventoryScreenQuickAccessPanel __instance, InventoryController inventoryController,
            GamePlayerOwner owner)
        {
            try
            {
                EnsureExtraSlots(__instance, inventoryController);
                if (owner != null)
                {
                    QuickSlotHotkeyHandler.RegisterBattlePanel(__instance);
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Failed to add extra quick slots: {ex}");
            }
        }

        private static void EnsureExtraSlots(InventoryScreenQuickAccessPanel panel, InventoryController inventoryController)
        {
            var boundItems = (IDictionary<EBoundItem, BoundItemView>)BoundItemsField.GetValue(panel);
            if (boundItems == null ||
                !boundItems.TryGetValue(EBoundItem.Item10, out BoundItemView template) ||
                template == null)
            {
                return;
            }
            Vector2 delta = new Vector2(75f, 0f);
            var templateRect = (RectTransform)template.transform;
            if (boundItems.TryGetValue(EBoundItem.Item9, out BoundItemView previous) && previous != null)
            {
                delta = templateRect.anchoredPosition - ((RectTransform)previous.transform).anchoredPosition;
            }

            Transform parent = template.transform.parent;
            bool hasLayoutGroup = parent.GetComponent<LayoutGroup>() != null;

            for (int i = 0; i < Plugin.ExtraSlotCount.Value; i++)
            {
                var key = (EBoundItem)(Plugin.FirstExtendedValue + i);
                if (boundItems.TryGetValue(key, out BoundItemView existing) && existing != null)
                {
                    continue;
                }

                BoundItemView clone = UnityEngine.Object.Instantiate(template, parent, false);
                clone.name = $"Bound Item {Plugin.FirstExtendedValue + i} (MoreQuickSlots)";

                var cloneRect = (RectTransform)clone.transform;
                cloneRect.SetSiblingIndex(template.transform.GetSiblingIndex() + 1 + i);
                if (!hasLayoutGroup)
                {
                    cloneRect.anchoredPosition = templateRect.anchoredPosition + delta * (i + 1);
                }

                CleanupClone(clone);
                boundItems[key] = clone;
            }

            TrimSlotBackgrounds(boundItems, template, delta.x);
            RemoveExcessSlots(boundItems, inventoryController);
        }
        private static void RemoveExcessSlots(IDictionary<EBoundItem, BoundItemView> boundItems,
            InventoryController inventoryController)
        {
            for (int value = Plugin.FirstExtendedValue + Plugin.ExtraSlotCount.Value;
                 value < Plugin.FirstExtendedValue + Plugin.MaxExtraSlots; value++)
            {
                var key = (EBoundItem)value;

                Item bound = inventoryController?.Inventory?.FastAccess?.GetBoundItem(key);
                if (bound != null)
                {
                    inventoryController.UnbindItem(key, null);
                    Plugin.LogSource.LogInfo($"Unbound {bound} from removed extra slot {value}");
                }

                if (boundItems.TryGetValue(key, out BoundItemView view))
                {
                    boundItems.Remove(key);
                    if (view != null)
                    {
                        UnityEngine.Object.Destroy(view.gameObject);
                    }
                }
            }
        }
        private static void TrimSlotBackgrounds(IDictionary<EBoundItem, BoundItemView> boundItems,
            BoundItemView template, float tileWidth)
        {
            if (tileWidth <= 0f)
            {
                return;
            }

            TrimBackground(template, tileWidth);
            int lastValue = Plugin.FirstExtendedValue + Plugin.ExtraSlotCount.Value - 1;
            for (int value = Plugin.FirstExtendedValue; value < lastValue; value++)
            {
                if (boundItems.TryGetValue((EBoundItem)value, out BoundItemView view) && view != null)
                {
                    TrimBackground(view, tileWidth);
                }
            }
        }

        private static void TrimBackground(BoundItemView view, float tileWidth)
        {
            var rect = view.transform.Find("Dark Background") as RectTransform;
            if (rect == null)
            {
                return;
            }

            float overhang = rect.rect.width - tileWidth;
            if (overhang <= 0.5f)
            {
                return;
            }

            if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x))
            {
                rect.sizeDelta -= new Vector2(overhang, 0f);
                rect.anchoredPosition -= new Vector2(overhang * (1f - rect.pivot.x), 0f);
            }
            else
            {
                rect.offsetMax -= new Vector2(overhang, 0f);
            }
        }
        private static void CleanupClone(BoundItemView clone)
        {
            if (InstallPlaceField.GetValue(clone) is Image installPlace)
            {
                for (int i = installPlace.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(installPlace.transform.GetChild(i).gameObject);
                }
            }

            if (CaptionField.GetValue(clone) is TMPro.TextMeshProUGUI caption)
            {
                caption.text = string.Empty;
            }

            clone.ShowArrow(false);
        }
    }
    [HarmonyPatch(typeof(ControlSettingsGroup), nameof(ControlSettingsGroup.GetBoundItemNames))]
    internal static class GetBoundItemNamesPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EBoundItem boundItem, ref string __result)
        {
            int value = (int)boundItem;
            if (value < Plugin.FirstExtendedValue)
            {
                return true;
            }

            __result = Plugin.GetSlotLabel(value - Plugin.FirstExtendedValue);
            return false;
        }
    }
}
