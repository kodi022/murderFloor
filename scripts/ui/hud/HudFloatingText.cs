namespace Shooter.Ui;

public partial class HudFloatingText : Control
{
    private static HudFloatingText current;

    private static Dictionary<Node3D, FloatingTextInfo> floatingTexts = [];

    [Export]
    private Control control;

    public static bool HasFloatingText(Node3D node) => floatingTexts.ContainsKey(node);

    public static bool AddFloatingText(Node3D node, string text, Vector3 offset)
    {
        if (HasFloatingText(node)) return false;

        var info = new FloatingTextInfo(text, offset, (Control)current.control.Duplicate());
        Global.GameManager.ClearOnLoad.AddChild(info.Control);
        return floatingTexts.TryAdd(node, info);
    }

    public static bool RemoveFloatingText(Node3D node)
    {
        if (HasFloatingText(node))
        {
            var val = floatingTexts[node];
            if (IsInstanceValid(val.Control)) val.Control.Free();
            return floatingTexts.Remove(node);
        }

        return false;
    }

    public static void ClearFloatingTexts()
    {
        foreach (var val in floatingTexts)
        {
            if (IsInstanceValid(val.Value.Control)) val.Value.Control.Free();
        }
        floatingTexts.Clear();
    }

    public override void _EnterTree()
    {
        current = this;
    }

    public override void _Ready()
    {
        control.Visible = false;
    }

    public override void _Process(double delta)
    {
        var margin = new Vector2(20, 20);
        var viewportSize = GetViewportRect().Size;
        foreach (var val in floatingTexts)
        {
            if (!IsInstanceValid(val.Value.Control) || !IsInstanceValid(val.Key))
            {
                RemoveFloatingText(val.Key);
                continue;
            }

            if (Game.Player.Self.Camera.IsPositionInFrustum(val.Key.GlobalPosition + val.Value.Offset))
            {
                val.Value.Control.Visible = true;
                val.Value.Control.GetChild<Label>(0).Text = val.Value.Text;
                var unproject = Game.Player.Self.Camera.UnprojectPosition(val.Key.GlobalPosition + val.Value.Offset);
                val.Value.Control.Position = unproject.Clamp(margin, viewportSize - margin);
            }
            else
            {
                // ! do later
                val.Value.Control.Visible = false;
            }
        }
    }

    public struct FloatingTextInfo(string text, Vector3 offset, Control control)
    {
        public string Text { get; set; } = text;
        public Vector3 Offset { get; set; } = offset;
        public Control Control { get; set; } = control;
    }
}
