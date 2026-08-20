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
        public UiContext(RelicCatalog catalog, RelicPresentationLibrary presentation, Inventory inventory,
            AcquisitionCoordinator coordinator, StatePersistence persistence)
        {
            Catalog = catalog;
            Presentation = presentation;
            Inventory = inventory;
            Coordinator = coordinator;
            Persistence = persistence;
        }

        public RelicCatalog Catalog { get; }

        public RelicPresentationLibrary Presentation { get; }

        public Inventory Inventory { get; }

        public AcquisitionCoordinator Coordinator { get; }

        public StatePersistence Persistence { get; }
    }
}
