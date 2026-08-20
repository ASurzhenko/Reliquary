using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Every set that exists in the game, indexed by id. Built once; never mutated afterwards — the same
    /// arrangement the relic catalogue uses.
    /// </summary>
    public sealed class SetCatalog
    {
        private static readonly RelicSet[] NoSets = Array.Empty<RelicSet>();

        private readonly Dictionary<SetId, RelicSet> _byId;
        private readonly RelicSet[] _ordered;
        private readonly Dictionary<RelicId, RelicSet[]> _byMember;

        private SetCatalog(Dictionary<SetId, RelicSet> byId, RelicSet[] ordered,
            Dictionary<RelicId, RelicSet[]> byMember)
        {
            _byId = byId;
            _ordered = ordered;
            _byMember = byMember;
        }

        /// <summary>
        /// Builds a catalogue from whatever the content layer produced. A set naming a relic this build does
        /// not have is KEPT with its member intact and reported: shrinking the goal instead would grant a
        /// perk for three quarters of a set. The editor content validator is what keeps that state out of a
        /// committed project.
        /// </summary>
        public static SetCatalog Create(IEnumerable<RelicSet> sets, RelicCatalog relics,
            out IReadOnlyList<RelicContentIssue> issues)
        {
            if (relics == null)
            {
                throw new ArgumentNullException(nameof(relics));
            }

            List<RelicContentIssue> found = new List<RelicContentIssue>();
            Dictionary<SetId, RelicSet> byId = new Dictionary<SetId, RelicSet>();

            if (sets != null)
            {
                foreach (RelicSet set in sets)
                {
                    // Rule 1
                    if (set == null)
                    {
                        found.Add(RelicContentIssue.Error(default, "A null set was handed to the catalogue and was skipped."));
                        continue;
                    }

                    // Rule 2 — which of the two survives is not defined; the caller's order is not guaranteed.
                    if (byId.ContainsKey(set.Id))
                    {
                        found.Add(RelicContentIssue.Error(default,
                            $"Two sets share the id '{set.Id}'. One of them was skipped — which one is not defined. " +
                            "The editor content validator reports this case with both asset paths."));
                        continue;
                    }

                    // Rule 4 — a set with nothing in it is not a goal.
                    if (set.Members.Count == 0)
                    {
                        found.Add(RelicContentIssue.Error(default,
                            $"Set '{set.Id}' lists no members and was skipped. A set with nothing in it cannot be completed."));
                        continue;
                    }

                    // Rule 5 — progress still tracks; there is simply nothing to grant.
                    if (set.Perks.Count == 0)
                    {
                        found.Add(RelicContentIssue.Warning(default,
                            $"Set '{set.Id}' grants no perk. Progress is still tracked and completing it still announces."));
                    }

                    // Rule 3 — kept, member and all, and said out loud. The set is now uncompletable.
                    for (int i = 0; i < set.Members.Count; i++)
                    {
                        RelicId member = set.Members[i];

                        if (relics.Contains(member))
                        {
                            continue;
                        }

                        found.Add(RelicContentIssue.Error(member,
                            $"Set '{set.Id}' lists '{member}', which is not in this build's catalogue. " +
                            "The member is kept, so the set can no longer be completed."));
                    }

                    byId.Add(set.Id, set);
                }
            }

            RelicSet[] ordered = new RelicSet[byId.Count];
            byId.Values.CopyTo(ordered, 0);
            Array.Sort(ordered, (left, right) => string.CompareOrdinal(left.Id.ToString(), right.Id.ToString()));

            issues = found;
            return new SetCatalog(byId, ordered, BuildMemberIndex(ordered));
        }

        public int Count => _ordered.Length;

        /// <summary>Accepted sets in a stable order: ordinal by id, independent of input order.</summary>
        public IReadOnlyList<RelicSet> All => _ordered;

        public bool TryGet(SetId id, out RelicSet set) => _byId.TryGetValue(id, out set);

        /// <summary>Every set that lists this relic. Empty for a relic in no set — a normal state.</summary>
        public IReadOnlyList<RelicSet> SetsContaining(RelicId relic)
        {
            return _byMember.TryGetValue(relic, out RelicSet[] sets) ? sets : NoSets;
        }

        /// <summary>The ids of every set this inventory currently completes. The watcher's seed.</summary>
        public IEnumerable<SetId> CompleteIn(Inventory inventory)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            for (int i = 0; i < _ordered.Length; i++)
            {
                if (SetProgress.For(_ordered[i], inventory).IsComplete)
                {
                    yield return _ordered[i].Id;
                }
            }
        }

        private static Dictionary<RelicId, RelicSet[]> BuildMemberIndex(RelicSet[] ordered)
        {
            Dictionary<RelicId, List<RelicSet>> building = new Dictionary<RelicId, List<RelicSet>>();

            for (int i = 0; i < ordered.Length; i++)
            {
                RelicSet set = ordered[i];

                for (int member = 0; member < set.Members.Count; member++)
                {
                    RelicId id = set.Members[member];

                    if (!building.TryGetValue(id, out List<RelicSet> holders))
                    {
                        holders = new List<RelicSet>(1);
                        building.Add(id, holders);
                    }

                    if (!holders.Contains(set))
                    {
                        holders.Add(set);
                    }
                }
            }

            Dictionary<RelicId, RelicSet[]> index = new Dictionary<RelicId, RelicSet[]>(building.Count);

            foreach (KeyValuePair<RelicId, List<RelicSet>> pair in building)
            {
                index.Add(pair.Key, pair.Value.ToArray());
            }

            return index;
        }
    }
}
