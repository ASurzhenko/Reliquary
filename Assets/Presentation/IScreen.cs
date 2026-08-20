namespace Reliquary.Presentation
{
    /// <summary>
    /// What the shell can do to a screen. The shell owns every domain subscription and pushes through this,
    /// because only one screen is active at a time and an inactive one would be deaf to the event that
    /// matters most: the one raised while the player was looking somewhere else.
    /// </summary>
    public interface IScreen
    {
        /// <summary>
        /// Re-read the domain and redraw. Called whether the screen is active or not, so it may not animate
        /// and may not assume its objects are enabled.
        /// </summary>
        void Refresh();

        /// <summary>Called when the screen becomes the visible one.</summary>
        void OnShown();
    }
}
