using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    /// <summary>
    /// An acquisition service the test completes by hand. Two behaviours matter: one that honours the
    /// cancellation token (a well-behaved local service) and one that ignores it (an authoritative service
    /// whose grant was already committed when the client gave up).
    /// </summary>
    internal sealed class FakeAcquisitionService : IAcquisitionService
    {
        private readonly List<TaskCompletionSource<AcquisitionResult>> _requests =
            new List<TaskCompletionSource<AcquisitionResult>>();

        /// <summary>When true, a cancelled token resolves the pending request as Cancelled.</summary>
        public bool HonoursCancellation { get; set; }

        /// <summary>When set, the returned task faults with this exception instead of resolving.</summary>
        public Exception FailsWith { get; set; }

        public int RequestCount => _requests.Count;

        public Task<AcquisitionResult> AcquireAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<AcquisitionResult> request = new TaskCompletionSource<AcquisitionResult>();
            _requests.Add(request);

            if (FailsWith != null)
            {
                request.SetException(FailsWith);
                return request.Task;
            }

            if (HonoursCancellation)
            {
                cancellationToken.Register(() => request.TrySetResult(AcquisitionResult.Cancelled()));
            }

            return request.Task;
        }

        public void Complete(int index, AcquisitionResult result)
        {
            _requests[index].TrySetResult(result);
        }

        public void CompleteLatest(AcquisitionResult result)
        {
            Complete(_requests.Count - 1, result);
        }
    }
}
