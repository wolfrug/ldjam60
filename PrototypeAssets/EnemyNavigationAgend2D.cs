using Godot;
using System;

public partial class EnemyNavigationAgent2D : BaseNavigationAgent2D
{
    protected override void HandleScannedBodies()
    {
        foreach (Node2D body in _bodiesInScanArea)
        {
            if (body is PlayerTopDown)
            {
                _rayCast.TargetPosition = _rayCast.ToLocal(body.GlobalPosition);
                GD.Print("Scanning for player");
                if (_rayCast.IsColliding())
                {
                    GD.Print("Raycast collided with: " + _rayCast.GetCollider());
                    if (_rayCast.GetCollider() is PlayerTopDown)
                    {
                        _navigationTarget = _rayCast.GetCollider() as PlayerTopDown;
                    }
                    else
                    {
                        _navigationTarget = null;
                    }
                }
                else
                {
                    _navigationTarget = null;
                }
            }
        }
    }

}
