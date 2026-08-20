using System;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Infrastructure
{
    /// <summary>
    /// Keeps the snapshot in PlayerPrefs, encoded with JsonUtility. All I/O and no rules: whether a decoded
    /// snapshot makes sense is SavedInventoryReader's judgement, which is why that half lives in the domain.
    /// </summary>
    public sealed class PlayerPrefsInventoryStore : IInventoryStore
    {
        /// <summary>
        /// Public because the editor save tools compile into a different assembly and read the same key.
        /// One declaration, so the two cannot drift apart.
        /// </summary>
        public static readonly string SaveKey = "reliquary.inventory";

        private readonly Func<bool> _refuseWrites;

        /// <param name="refuseWrites">
        /// Diagnostics only, and null in a player build: lets a session refuse every write so the failure
        /// path can be driven by hand. Read at write time rather than captured, because the acceptance run
        /// flips it without leaving play mode.
        /// </param>
        public PlayerPrefsInventoryStore(Func<bool> refuseWrites = null)
        {
            _refuseWrites = refuseWrites;
        }

        public StoredState Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return StoredState.None();
            }

            string payload = PlayerPrefs.GetString(SaveKey, string.Empty);

            if (string.IsNullOrWhiteSpace(payload))
            {
                return StoredState.Unreadable("the saved payload is empty");
            }

            InventorySnapshot snapshot;

            try
            {
                snapshot = JsonUtility.FromJson<InventorySnapshot>(payload);
            }
            catch (ArgumentException exception)
            {
                return StoredState.Unreadable($"the saved payload is not valid JSON: {exception.Message}");
            }

            if (snapshot == null)
            {
                return StoredState.Unreadable("the saved payload decoded to nothing");
            }

            return StoredState.Loaded(snapshot);
        }

        public bool TrySave(InventorySnapshot snapshot, out string failure)
        {
            if (_refuseWrites != null && _refuseWrites())
            {
                failure = "write refused (diagnostics)";
                return false;
            }

            if (snapshot == null)
            {
                failure = "there was no snapshot to write";
                return false;
            }

            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(snapshot));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                failure = $"the save could not be written: {exception.Message}";
                return false;
            }

            failure = null;
            return true;
        }
    }
}
