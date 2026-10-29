namespace Shooter.Game;

public partial class Resupply : Node3D
{
    private bool used = false;
    private PackedScene ammoBoxPreload;

    public override void _Ready()
    {
        ammoBoxPreload ??= GD.Load<PackedScene>("res://scenes/pickup/Ammobox.tscn");

        GetChild<Usable>(0).UseAction = () =>
        {
            if (used) return;
            used = true;
            GetChild<Usable>(0).UseInfoText = "";

            var ammobox = (Node3D)ammoBoxPreload.Instantiate();
            Global.GameManager.ClearOnLoad.AddChild(ammobox);
            ammobox.GlobalPosition = ((Node3D)GetChild(0).GetChild(0)).GlobalPosition + new Vector3(MakeRand(1f), 1.5f, MakeRand(1f));

            ((RigidBody3D)ammobox.GetChild(0).GetChild(0)).LinearVelocity = new Vector3(MakeRand(2f), 3f, MakeRand(2f));
            ((RigidBody3D)ammobox.GetChild(0).GetChild(0)).AngularVelocity = Vector3.One * MakeRand(2f);
        };

        Game.Current.GameWaveStart += (a) => { QueueFree(); };
    }

    private static float MakeRand(float scale) => Random.Shared.NextSingle() * scale - (scale / 2);
}