using UnityEditor;
using UnityEngine;
using Reliquary.Infrastructure;

namespace Reliquary.Infrastructure.EditorTools
{
    /// <summary>
    /// Reads and corrupts the save on purpose. The read path branches on data this build did not write, and
    /// a branch that has only ever been driven in the accepting direction is not yet a gate — these menu
    /// items are how each refusal gets driven for real. The same payloads are literals in the codec tests.
    /// </summary>
    internal static class InventorySaveTools
    {
        private static readonly string MenuRoot = "Tools/Reliquary/Save/";

        [MenuItem("Tools/Reliquary/Save/Inspect")]
        private static void Inspect()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsInventoryStore.SaveKey))
            {
                Debug.Log($"{nameof(InventorySaveTools)}.{nameof(Inspect)} no save exists under '{PlayerPrefsInventoryStore.SaveKey}'.");
                return;
            }

            Debug.Log($"{nameof(InventorySaveTools)}.{nameof(Inspect)} payload: {PlayerPrefs.GetString(PlayerPrefsInventoryStore.SaveKey, string.Empty)}");
        }

        [MenuItem("Tools/Reliquary/Save/Clear")]
        private static void Clear()
        {
            bool confirmed = EditorUtility.DisplayDialog("Clear the saved inventory?",
                $"This deletes '{PlayerPrefsInventoryStore.SaveKey}' from PlayerPrefs. The player's copies go with it.",
                "Delete it", "Keep it");

            if (!confirmed)
            {
                return;
            }

            PlayerPrefs.DeleteKey(PlayerPrefsInventoryStore.SaveKey);
            PlayerPrefs.Save();
            Debug.Log($"{nameof(InventorySaveTools)}.{nameof(Clear)} the save was deleted.");
        }

        [MenuItem("Tools/Reliquary/Save/Break — invalid JSON")]
        private static void BreakWithInvalidJson()
        {
            Write("{\"Version\":1,\"Entries\":[{\"RelicId\":");
        }

        [MenuItem("Tools/Reliquary/Save/Break — future version")]
        private static void BreakWithFutureVersion()
        {
            Write("{\"Version\":99,\"Entries\":[]}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — no entries array")]
        private static void BreakWithNoEntriesArray()
        {
            Write("{\"Version\":1}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — empty entries array")]
        private static void BreakWithEmptyEntriesArray()
        {
            Write("{\"Version\":1,\"Entries\":[]}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — unknown relic")]
        private static void BreakWithUnknownRelic()
        {
            Write("{\"Version\":1,\"Entries\":[{\"RelicId\":\"relic.sunken_crown\",\"Count\":1}," +
                "{\"RelicId\":\"relic.not_in_this_build\",\"Count\":3}]}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — essence with no entries array")]
        private static void BreakWithEssenceAndNoEntriesArray()
        {
            // The reader accepts this now — essence alone is state worth restoring — so the null entries
            // array has to be normalised rather than dereferenced. This is the only way to drive that.
            Write("{\"Version\":1,\"Essence\":100}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — P3-era payload (no Essence field)")]
        private static void BreakWithPayloadThatPredatesEssence()
        {
            Write("{\"Version\":1,\"Entries\":[{\"RelicId\":\"relic.sunken_crown\",\"Count\":2}]}");
        }

        [MenuItem("Tools/Reliquary/Save/Break — malformed entries")]
        private static void BreakWithMalformedEntries()
        {
            Write("{\"Version\":1,\"Entries\":[{\"RelicId\":\"  \",\"Count\":2}," +
                "{\"RelicId\":\"relic.ember_chalice\",\"Count\":0}," +
                "{\"RelicId\":\"relic.sunken_crown\",\"Count\":3}," +
                "{\"RelicId\":\"relic.sunken_crown\",\"Count\":5}]}");
        }

        private static void Write(string payload)
        {
            PlayerPrefs.SetString(PlayerPrefsInventoryStore.SaveKey, payload);
            PlayerPrefs.Save();
            Debug.Log($"{nameof(InventorySaveTools)}.{nameof(Write)} [{MenuRoot}] the save now reads: {payload}");
        }
    }
}
