using System;
using System.Collections.Generic;

namespace Reliquary.Domain
{
    /// <summary>
    /// Every relic that exists in the game, indexed by id. Built once; never mutated afterwards.
    /// </summary>
    public sealed class RelicCatalog
    {
        private readonly Dictionary<RelicId, Relic> _byId;
        private readonly Relic[] _ordered;

        private RelicCatalog(Dictionary<RelicId, Relic> byId, Relic[] ordered)
        {
            _byId = byId;
            _ordered = ordered;
        }

        /// <summary>
        /// Builds a catalogue from whatever the content layer produced. Entries that cannot be accepted are
        /// skipped and reported — never dropped silently. When two relics claim one id, one of them is kept
        /// and which one is not defined: the caller's input order is not guaranteed. The editor content
        /// validator is what keeps this state out of a committed project.
        /// </summary>
        public static RelicCatalog Create(IEnumerable<Relic> relics, out IReadOnlyList<RelicContentIssue> issues)
        {
            List<RelicContentIssue> found = new List<RelicContentIssue>();
            Dictionary<RelicId, Relic> byId = new Dictionary<RelicId, Relic>();

            if (relics != null)
            {
                foreach (Relic relic in relics)
                {
                    if (relic == null)
                    {
                        found.Add(RelicContentIssue.Error(default, "A null relic was handed to the catalogue and was skipped."));
                        continue;
                    }

                    if (byId.ContainsKey(relic.Id))
                    {
                        found.Add(RelicContentIssue.Error(relic.Id,
                            $"Two relics share the id '{relic.Id}'. One of them was skipped — which one is not defined. " +
                            "The editor content validator reports this case with both asset paths."));
                        continue;
                    }

                    byId.Add(relic.Id, relic);
                }
            }

            Relic[] ordered = new Relic[byId.Count];
            byId.Values.CopyTo(ordered, 0);
            Array.Sort(ordered, (left, right) => string.CompareOrdinal(left.Id.ToString(), right.Id.ToString()));

            issues = found;
            return new RelicCatalog(byId, ordered);
        }

        public int Count => _ordered.Length;

        /// <summary>
        /// Accepted relics in a stable order: ordinal by id, independent of input order. These are the exact
        /// instances the caller handed in, so a caller may pair them back to whatever produced them.
        /// </summary>
        public IReadOnlyList<Relic> All => _ordered;

        public bool Contains(RelicId id) => _byId.ContainsKey(id);

        public bool TryGet(RelicId id, out Relic relic) => _byId.TryGetValue(id, out relic);
    }
}
