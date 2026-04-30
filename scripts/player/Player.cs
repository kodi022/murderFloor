using Godot;
using System;

namespace MurderFloor;

public partial class Player : CharacterBody3D
{
    private Vector3 lastVel;

    public override void _Process(double delta)
    {
        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");

        var wishMove = new Vector3(forward, 0, strafe);

        Velocity = wishMove;
        MoveAndSlide();
        lastVel = Velocity;
    }
}
