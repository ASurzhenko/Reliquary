using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class AcquisitionCoordinatorTests
    {
        [Test]
        public async Task FreshRequest_WritesAndReportsGranted()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.sunken_crown")));
            AcquisitionCompletion completion = await request;

            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Granted));
            Assert.That(completion.WasFirstCopy, Is.True);
            Assert.That(inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(1));
        }

        [Test]
        public async Task SupersededRequest_DoesNotWrite_AndReportsSuperseded()
        {
            // A service that ignores the token: an authoritative grant cannot be un-issued, so the counter is
            // the only thing that can stop it from applying to a session that moved on.
            FakeAcquisitionService service = new FakeAcquisitionService { HonoursCancellation = false };
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            Task<AcquisitionCompletion> first = coordinator.RequestAsync();
            Task<AcquisitionCompletion> second = coordinator.RequestAsync();

            service.Complete(0, AcquisitionResult.Granted(new RelicId("relic.sunken_crown")));
            service.Complete(1, AcquisitionResult.Granted(new RelicId("relic.pyre_key")));

            AcquisitionCompletion older = await first;
            AcquisitionCompletion newer = await second;

            Assert.That(older.Reason, Is.EqualTo(AcquisitionCompletionReason.Superseded));
            Assert.That(older.Detail, Does.Contain("1").And.Contain("2"));
            Assert.That(newer.Reason, Is.EqualTo(AcquisitionCompletionReason.Granted));
            Assert.That(inventory.DistinctCount, Is.EqualTo(1));
            Assert.That(inventory.Owns(new RelicId("relic.sunken_crown")), Is.False);
        }

        [Test]
        public async Task CancelledRequest_DoesNotWrite_AndReportsCancelled()
        {
            // A service that honours the token reports Cancelled, and the two safeguards stay distinguishable.
            FakeAcquisitionService service = new FakeAcquisitionService { HonoursCancellation = true };
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            coordinator.CancelPending();
            AcquisitionCompletion completion = await request;

            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Cancelled));
            Assert.That(inventory.DistinctCount, Is.EqualTo(0));
        }

        [Test]
        public async Task SupersededRequest_DoesNotUndoTheNewerOne()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            Task<AcquisitionCompletion> first = coordinator.RequestAsync();
            Task<AcquisitionCompletion> second = coordinator.RequestAsync();

            // The newer request commits first; the older one then comes back and must leave it alone.
            service.Complete(1, AcquisitionResult.Granted(new RelicId("relic.pyre_key")));
            await second;

            service.Complete(0, AcquisitionResult.Granted(new RelicId("relic.sunken_crown")));
            await first;

            Assert.That(inventory.CountOf(new RelicId("relic.pyre_key")), Is.EqualTo(1));
            Assert.That(inventory.DistinctCount, Is.EqualTo(1));
        }

        [Test]
        public async Task RequestAfterCancel_StillCommits()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            // A cancel that fired before this request must not invalidate it: the counter is a value each
            // request captures, not a flag it reads later.
            coordinator.CancelPending();

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.sunken_crown")));
            AcquisitionCompletion completion = await request;

            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Granted));
            Assert.That(inventory.CountOf(new RelicId("relic.sunken_crown")), Is.EqualTo(1));
        }

        [Test]
        public async Task ServiceThrows_ReportsFailedAndDoesNotWrite()
        {
            FakeAcquisitionService service = new FakeAcquisitionService
            {
                FailsWith = new InvalidOperationException("the dig collapsed")
            };
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            AcquisitionCompletion completion = await coordinator.RequestAsync();

            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Failed));
            Assert.That(completion.Detail, Does.Contain("the dig collapsed"));
            Assert.That(inventory.DistinctCount, Is.EqualTo(0));
        }

        [Test]
        public async Task ServiceThrowsOperationCanceled_ReportsCancelled()
        {
            FakeAcquisitionService service = new FakeAcquisitionService
            {
                FailsWith = new OperationCanceledException()
            };
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            AcquisitionCompletion completion = await coordinator.RequestAsync();

            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Cancelled));
            Assert.That(inventory.DistinctCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GrantedIdNotInCatalogue_ReportsFailedAndDoesNotWrite()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.not_in_this_build")));
            AcquisitionCompletion completion = await request;

            // Without this check Inventory.Add throws out of RequestAsync and no terminal is raised at all.
            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Failed));
            Assert.That(completion.Detail, Does.Contain("relic.not_in_this_build"));
            Assert.That(inventory.DistinctCount, Is.EqualTo(0));
        }

        [Test]
        public async Task EveryTerminal_RaisesCompletedExactlyOnce()
        {
            // Every shape a service can answer with, plus the grant this build cannot honour. The superseded
            // terminal has its own test, because reaching it needs a second request.
            await AssertOneTerminal(AcquisitionCompletionReason.Granted,
                service => service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.sunken_crown"))));
            await AssertOneTerminal(AcquisitionCompletionReason.Rejected,
                service => service.CompleteLatest(AcquisitionResult.Rejected(AcquisitionRejection.CatalogueEmpty)));
            await AssertOneTerminal(AcquisitionCompletionReason.Cancelled,
                service => service.CompleteLatest(AcquisitionResult.Cancelled()));
            await AssertOneTerminal(AcquisitionCompletionReason.Failed,
                service => service.CompleteLatest(AcquisitionResult.Failed("something gave way")));
            await AssertOneTerminal(AcquisitionCompletionReason.Failed,
                service => service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.not_in_this_build"))));
        }

        [Test]
        public void IsBusy_IsFalseAfterCancelPending()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, new Inventory(), Catalog());

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            Assert.That(coordinator.IsBusy, Is.True);

            coordinator.CancelPending();

            // An invalidated request is not "busy": it can no longer write, and reporting it as busy would
            // leave a caller waiting on something that has already been decided.
            Assert.That(coordinator.IsBusy, Is.False);

            service.CompleteLatest(AcquisitionResult.Cancelled());
            Assert.That(request.IsCompleted, Is.True);
        }

        [Test]
        public async Task DisposedCoordinator_RaisesNoTerminal()
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            Inventory inventory = new Inventory();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, inventory, Catalog());
            int raised = 0;
            coordinator.Completed += _ => raised++;

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            coordinator.Dispose();
            service.CompleteLatest(AcquisitionResult.Granted(new RelicId("relic.sunken_crown")));
            AcquisitionCompletion completion = await request;

            Assert.That(raised, Is.EqualTo(0), "Dispose clears the subscribers, so a late result reaches nobody");
            Assert.That(completion.Reason, Is.EqualTo(AcquisitionCompletionReason.Superseded));
            Assert.That(inventory.DistinctCount, Is.EqualTo(0));
        }

        private static async Task AssertOneTerminal(AcquisitionCompletionReason expected,
            Action<FakeAcquisitionService> resolve)
        {
            FakeAcquisitionService service = new FakeAcquisitionService();
            AcquisitionCoordinator coordinator = new AcquisitionCoordinator(service, new Inventory(), Catalog());
            List<AcquisitionCompletion> raised = new List<AcquisitionCompletion>();
            coordinator.Completed += completion => raised.Add(completion);

            Task<AcquisitionCompletion> request = coordinator.RequestAsync();
            resolve(service);
            AcquisitionCompletion returned = await request;

            Assert.That(raised.Count, Is.EqualTo(1), $"expected exactly one {expected} terminal");
            Assert.That(raised[0].Reason, Is.EqualTo(expected));
            Assert.That(returned.Reason, Is.EqualTo(expected), "the awaiting caller gets the same terminal");
        }

        private static RelicCatalog Catalog()
        {
            return RelicCatalog.Create(new[]
            {
                new Relic(new RelicId("relic.sunken_crown"), 10, 100, Array.Empty<IRelicEffect>()),
                new Relic(new RelicId("relic.pyre_key"), 10, 100, Array.Empty<IRelicEffect>())
            }, out _);
        }
    }
}
