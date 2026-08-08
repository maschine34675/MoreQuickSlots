using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace MoreQuickSlots
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.maschine.MoreQuickSlots";
        public const string PluginName = "maschine-MoreQuickSlots";
        public const string PluginVersion = "2.0.0";
        public const int FirstExtendedValue = 12;
        public const int MaxExtraSlots = 6;

        public static ManualLogSource LogSource;
        public static ConfigEntry<int> ExtraSlotCount;
        public static readonly ConfigEntry<KeyboardShortcut>[] SlotKeys = new ConfigEntry<KeyboardShortcut>[MaxExtraSlots];

        private Harmony _harmony;

        private void Awake()
        {
            LogSource = Logger;

            ExtraSlotCount = Config.Bind("General", "ExtraSlotCount", 3,
                new ConfigDescription(
                    "Number of additional quick slots (next to the default 7). Takes effect when the inventory is " +
                    "next opened. Reducing the count removes the surplus slots and automatically unbinds their items.",
                    new AcceptableValueRange<int>(1, MaxExtraSlots)));

            KeyCode[] defaults = { KeyCode.Minus, KeyCode.Equals, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None };
            for (int i = 0; i < MaxExtraSlots; i++)
            {
                SlotKeys[i] = Config.Bind("Hotkeys", $"ExtraSlot{i + 1}", new KeyboardShortcut(defaults[i]),
                    $"Hotkey for extra quick slot {i + 1}. Press while hovering an item in the inventory to bind it, press in raid to use/equip the bound item.");
            }

            InstallJsonConverter();
            RegisterExtendedEnumNames();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            gameObject.AddComponent<QuickSlotHotkeyHandler>();

            LogSource.LogInfo($"{PluginName} {PluginVersion} loaded, {ExtraSlotCount.Value} extra quick slot(s).");
        }
        private static void InstallJsonConverter()
        {
            try
            {
                var converters = new List<JsonConverter> { new BoundItemStringConverter() };
                converters.AddRange(EftJsonConverters.Converters);
                JsonConverter[] newArray = converters.ToArray();

                AccessTools.Field(typeof(EftJsonConverters), nameof(EftJsonConverters.Converters))
                    .SetValue(null, newArray);
                EftJsonConverters.SerializerSettings.Converters = newArray;
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Failed to install EBoundItem json converter: {ex}");
            }
        }
        private static void RegisterExtendedEnumNames()
        {
            try
            {
                _ = EnumHelper<EBoundItem>.NameToValueMap;
                _ = EnumHelper<EBoundItem>.ValueToNameMap;

                for (int i = 0; i < MaxExtraSlots; i++)
                {
                    var key = (EBoundItem)(FirstExtendedValue + i);
                    string name = ((int)key).ToString();
                    if (!EnumHelper<EBoundItem>._nameValueMap.ContainsKey(name))
                    {
                        EnumHelper<EBoundItem>._nameValueMap.Add(name, key);
                    }
                    if (!EnumHelper<EBoundItem>._valueNameMap.ContainsKey(key))
                    {
                        EnumHelper<EBoundItem>._valueNameMap.Add(key, name);
                    }
                }
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Failed to register extended EBoundItem names: {ex}");
            }
        }
        public static string GetSlotLabel(int extIndex)
        {
            if (extIndex < 0 || extIndex >= MaxExtraSlots)
            {
                return "?";
            }

            KeyCode key = SlotKeys[extIndex].Value.MainKey;
            switch (key)
            {
                case KeyCode.None: return "?";
                case KeyCode.Period: return ".";
                case KeyCode.Equals: return "=";
                default: return KeyTools.GetKeyNameAlias(key);
            }
        }
    }
}
