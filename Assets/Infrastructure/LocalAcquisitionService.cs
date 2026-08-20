using System;
using System.Threading;
using System.Threading.Tasks;
using Reliquary.Domain;

namespace Reliquary.Infrastructure
{
    /// <summary>
    /// Draws a relic on this device, behind the seam an authoritative service would attach to. It is
    /// constructed with what it needs rather than told by its caller — a client that describes what it owns
    /// has already lost the argument with a server that knows.
    /// </summary>
    public sealed class LocalAcquisitionService : IAcquisitionService
    {
        private readonly RelicCatalog _catalog;
        private readonly SetCatalog _sets;
        private readonly Inventory _inventory;
        private readonly Random _random;
        private readonly int _excavationMilliseconds;
        private readonly Func<bool> _completeDespiteCancellation;

        /// <param name="completeDespiteCancellation">
        /// Diagnostics only, and null in a player build: reproduces an authoritative service whose grant was
        /// already committed when the client gave up, which is the case the coordinator's generation counter
        /// exists for.
        /// </param>
        public LocalAcquisitionService(RelicCatalog catalog, SetCatalog sets, Inventory inventory, Random random,
            int excavationMilliseconds, Func<bool> completeDespiteCancellation = null)
        {
            _catalog = catalog;
            _sets = sets;
            _inventory = inventory;
            _random = random;
            _excavationMilliseconds = excavationMilliseconds < 0 ? 0 : excavationMilliseconds;
            _completeDespiteCancellation = completeDespiteCancellation;
        }

        public async Task<AcquisitionResult> AcquireAsync(CancellationToken cancellationToken)
        {
            try
            {
                // No ConfigureAwait(false) anywhere in this project: the continuation must resume on Unity's
                // main thread, because everything downstream of it touches engine state.
                await Task.Delay(_excavationMilliseconds, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (_completeDespiteCancellation == null || !_completeDespiteCancellation())
                {
                    return AcquisitionResult.Cancelled();
                }
            }

            // Everything contributing right now — owned relics AND the perks of completed sets. This one line
            // is where a set's perk reaches the next draw.
            RelicModifiers modifiers = ActiveModifiers.For(_catalog, _sets, _inventory);
            DrawTable table = DrawTable.Build(_catalog, _inventory, modifiers);

            if (table.TotalWeight <= 0)
            {
                return AcquisitionResult.Rejected(AcquisitionRejection.CatalogueEmpty);
            }

            return AcquisitionResult.Granted(table.Pick(_random.Next(table.TotalWeight)));
        }
    }
}
