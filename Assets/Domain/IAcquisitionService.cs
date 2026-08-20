using System.Threading;
using System.Threading.Tasks;

namespace Reliquary.Domain
{
    /// <summary>
    /// Where a relic comes from. The caller does not describe the player's state — an implementation that
    /// trusted the caller for that would be the bug this seam exists to make impossible. A local
    /// implementation is constructed with what it needs; an authoritative one would be constructed with an
    /// endpoint, and this call site would not change.
    ///
    /// Implementations report cancellation as AcquisitionResult.Cancelled and failure as
    /// AcquisitionResult.Failed; they are not expected to throw. The coordinator defends against both
    /// anyway, and checks the granted id against the catalogue before accepting it.
    /// </summary>
    public interface IAcquisitionService
    {
        Task<AcquisitionResult> AcquireAsync(CancellationToken cancellationToken);
    }
}
