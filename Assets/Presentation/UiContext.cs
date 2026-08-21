using Reliquary.Content;
using Reliquary.Domain;

namespace Reliquary.Presentation
{
    /// <summary>
    /// Everything the UI is allowed to read, handed to it once by the composition root. It is a parameter
    /// object rather than a lookup: a view that needs the catalogue is given the catalogue, and nothing in
    /// this layer asks a static where its dependencies came from.
    /// </summary>
    public sealed class UiContext
    {
        public UiContext(RelicCatalog catalog, RelicPresentationLibrary presentation, SetCatalog sets,
            SetPresentationLibrary setPresentation, Inventory inventory, EssenceWallet wallet,
            EssenceExchange exchange, Trader trader, SetCompletionWatcher completion,
            AcquisitionCoordinator coordinator, StatePersistence persistence)
        {
            Catalog = catalog;
            Presentation = presentation;
            Sets = sets;
            SetPresentation = setPresentation;
            Inventory = inventory;
            Wallet = wallet;
            Exchange = exchange;
            Trader = trader;
            Completion = completion;
            Coordinator = coordinator;
            Persistence = persistence;
        }

        public RelicCatalog Catalog { get; }

        public RelicPresentationLibrary Presentation { get; }

        public SetCatalog Sets { get; }

        public SetPresentationLibrary SetPresentation { get; }

        public Inventory Inventory { get; }

        public EssenceWallet Wallet { get; }

        public EssenceExchange Exchange { get; }

        public Trader Trader { get; }

        public SetCompletionWatcher Completion { get; }

        public AcquisitionCoordinator Coordinator { get; }

        public StatePersistence Persistence { get; }
    }
}
