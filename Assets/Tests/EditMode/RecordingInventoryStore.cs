using System.Collections.Generic;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>A store that keeps every snapshot handed to it, and can be told to refuse the next write.</summary>
    internal sealed class RecordingInventoryStore : IInventoryStore
    {
        public List<InventorySnapshot> Saved { get; } = new List<InventorySnapshot>();

        public StoredState NextLoad { get; set; } = StoredState.None();

        public bool RefusesWrites { get; set; }

        public string Failure { get; set; } = "the store refused the write";

        public StoredState Load()
        {
            return NextLoad;
        }

        public bool TrySave(InventorySnapshot snapshot, out string failure)
        {
            if (RefusesWrites)
            {
                failure = Failure;
                return false;
            }

            Saved.Add(snapshot);
            failure = null;
            return true;
        }
    }
}
