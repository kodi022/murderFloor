namespace Shooter.Ui;

public partial class OpenUi : Control
{
    /// <summary>Free should be called by the end of this, otherwise the UI will stay</summary>
    public virtual void Close()
    {
        Free();
    }
}