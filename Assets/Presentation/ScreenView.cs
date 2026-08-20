namespace Reliquary.Presentation
{
    /// <summary>
    /// A tab's content. It is a class rather than only an interface so the tab bar can hold one in a
    /// serialized field — Unity cannot serialize an interface reference.
    /// </summary>
    public abstract class ScreenView : View, IScreen
    {
        public abstract void Refresh();

        public virtual void OnShown()
        {
        }
    }
}
