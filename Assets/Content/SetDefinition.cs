using System;
using System.Collections.Generic;
using UnityEngine;
using Reliquary.Domain;

namespace Reliquary.Content
{
    /// <summary>
    /// A set of relics and what completing it grants. The set lists its members; a relic carries no set
    /// field, so adding a set edits no relic asset. The perk is authored with the same effect asset type a
    /// relic uses — the gamification layer is a consumer of the effect system, not a mechanism beside it.
    /// </summary>
    [CreateAssetMenu(menuName = "Reliquary/Set", fileName = "Set_")]
    public sealed class SetDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 3)] private string _description;
        [SerializeField] private RelicDefinition[] _members;
        [SerializeField] private RelicEffectDefinition[] _perks;

        /// <summary>
        /// The id the set catalogue will carry. SetId does not trim, so this is the single place the trimming
        /// rule lives — the same arrangement RelicDefinition.TrimmedId uses for relics.
        /// </summary>
        public string TrimmedId => _id == null ? string.Empty : _id.Trim();

        public string DisplayName => _displayName;

        public string Description => _description;

        public IReadOnlyList<RelicDefinition> Members => _members ?? Array.Empty<RelicDefinition>();

        public IReadOnlyList<RelicEffectDefinition> Perks => _perks ?? Array.Empty<RelicEffectDefinition>();

        /// <summary>
        /// Converts this asset into the rules' view of a set. Returns false with a reason when the asset is
        /// not authored well enough to be one. Stops at the first problem: the editor validator, not this
        /// method, is what reports every problem on every asset. A member the catalogue does not contain is
        /// NOT refused here — that judgement needs the catalogue and belongs to SetCatalog.Create.
        /// </summary>
        public bool TryCreateSet(out RelicSet set, out string error)
        {
            set = null;

            if (string.IsNullOrWhiteSpace(_id))
            {
                error = "Id is empty. Give the set a stable id such as 'set.tideworn'.";
                return false;
            }

            IReadOnlyList<RelicDefinition> members = Members;

            if (members.Count == 0)
            {
                error = "The set lists no members. A set with nothing in it cannot be completed.";
                return false;
            }

            List<RelicId> memberIds = new List<RelicId>(members.Count);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < members.Count; i++)
            {
                RelicDefinition member = members[i];

                if (member == null)
                {
                    error = $"Member slot {i} is empty or points at an asset that no longer exists.";
                    return false;
                }

                string id = member.TrimmedId;

                if (id.Length == 0)
                {
                    error = $"Member slot {i} points at '{member.name}', which has no id.";
                    return false;
                }

                if (!seen.Add(id))
                {
                    error = $"'{id}' is listed twice. A member counts once, so the second slot only inflates the total.";
                    return false;
                }

                memberIds.Add(new RelicId(id));
            }

            IReadOnlyList<RelicEffectDefinition> perks = Perks;
            List<IRelicEffect> created = new List<IRelicEffect>(perks.Count);

            for (int i = 0; i < perks.Count; i++)
            {
                RelicEffectDefinition perk = perks[i];

                if (perk == null)
                {
                    error = $"Perk slot {i} is empty or points at an asset that no longer exists.";
                    return false;
                }

                created.Add(perk.CreateEffect());
            }

            set = new RelicSet(new SetId(TrimmedId), memberIds, created);
            error = null;
            return true;
        }
    }
}
