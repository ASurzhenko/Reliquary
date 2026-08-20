using System;
using System.Threading;
using System.Threading.Tasks;

namespace Reliquary.Domain
{
    /// <summary>
    /// Sequences acquisitions and owns the only write in the package. A continuation that resumes after the
    /// session moved on must not apply: the guard is one generation counter, held here — beside the write —
    /// rather than on a view that can be destroyed while a request is still in flight.
    /// </summary>
    public sealed class AcquisitionCoordinator : IDisposable
    {
        private readonly IAcquisitionService _service;
        private readonly Inventory _inventory;
        private readonly RelicCatalog _catalog;

        private int _generation;
        private CancellationTokenSource _pending;

        public AcquisitionCoordinator(IAcquisitionService service, Inventory inventory, RelicCatalog catalog)
        {
            _service = service;
            _inventory = inventory;
            _catalog = catalog;
        }

        /// <summary>
        /// Raised exactly once per request, for every terminal including a superseded one. Subscribers must
        /// not throw: this is a domain type with nothing to log to, so a throwing handler faults the
        /// caller's task.
        /// </summary>
        public event Action<AcquisitionCompletion> Completed;

        /// <summary>A request is in flight and has not been invalidated.</summary>
        public bool IsBusy => _pending != null && !_pending.IsCancellationRequested;

        public async Task<AcquisitionCompletion> RequestAsync()
        {
            int generation = ++_generation;
            CancellationTokenSource superseded = _pending;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _pending = cancellation;

            // Cancel the older request but do NOT dispose it: its own request owns its lifetime and disposes
            // it in the finally below, once nothing is registered on its token any more.
            superseded?.Cancel();

            AcquisitionResult result;

            try
            {
                result = await _service.AcquireAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                result = AcquisitionResult.Cancelled();
            }
            catch (Exception exception)
            {
                result = AcquisitionResult.Failed(exception.Message);
            }
            finally
            {
                if (ReferenceEquals(_pending, cancellation))
                {
                    _pending = null;
                }

                cancellation.Dispose();
            }

            // The result's own shape is read first so that a service which honoured the token reports
            // Cancelled rather than being absorbed into Superseded. Neither branch writes, so the order is a
            // diagnostic choice, not a safety one.
            if (result.Outcome != AcquisitionOutcome.Granted)
            {
                return Complete(AcquisitionCompletion.From(result));
            }

            // A grant this build cannot honour. The service is the replaceable half of this package, so an
            // id that is not in the catalogue is a response shape, not an impossibility — and Inventory.Add
            // would throw out of this method with no terminal raised at all.
            if (!result.RelicId.IsValid || !_catalog.Contains(result.RelicId))
            {
                return Complete(AcquisitionCompletion.Failed(
                    $"the acquisition granted '{result.RelicId}', which is not in this build's catalogue"));
            }

            // A grant that arrives after the session moved on. With a local service this is a narrow window;
            // with an authoritative one it is routine, because a committed grant cannot be un-issued.
            if (generation != _generation)
            {
                return Complete(AcquisitionCompletion.Superseded(generation, _generation, result.RelicId));
            }

            // No await may be introduced between the check above and the end of this call: the guard is only
            // atomic because nothing suspends in between. ConfigureAwait(false) is banned in this project for
            // the same reason — a continuation resuming off the main thread would write from a worker.
            InventoryChange change = _inventory.Add(result.RelicId);
            return Complete(AcquisitionCompletion.Granted(change));
        }

        /// <summary>
        /// Invalidates every request in flight: the counter moves, so no continuation may write, and the
        /// token is cancelled so a well-behaved service stops early. A request started after this call is
        /// unaffected — the counter is a value each request captures, not a flag.
        /// </summary>
        public void CancelPending()
        {
            _generation++;
            _pending?.Cancel();
        }

        public void Dispose()
        {
            CancelPending();
            Completed = null;
        }

        private AcquisitionCompletion Complete(AcquisitionCompletion completion)
        {
            Completed?.Invoke(completion);
            return completion;
        }
    }
}
