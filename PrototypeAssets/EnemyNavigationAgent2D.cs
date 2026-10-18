using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyNavigationAgent2D : BaseNavigationAgent2D
{
    [Export] public int _attackDamage = 1;
    [Export] public int _health = 1;
    [Export] public float _attackDistance = 20f;
    protected bool _isAttacking = false;
    protected override async void HandleScannedBodies()
    {
        foreach (Node2D body in _bodiesInScanArea)
        {
            if (body is PlayerTopDown)
            {
                _rayCast.TargetPosition = _rayCast.ToLocal(body.GlobalPosition);
                //GD.Print("Scanning for player");
                if (_rayCast.IsColliding())
                {
                    //GD.Print("Raycast collided with: " + _rayCast.GetCollider());
                    if (_rayCast.GetCollider() is PlayerTopDown)
                    {
                        if (_navigationTarget == null)
                        {
                            GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = "Detected" });
                        }
                        _navigationTarget = _rayCast.GetCollider() as PlayerTopDown;
                        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                        if (_navigationAgent.DistanceToTarget() <= _attackDistance)
                        {
                            HandleAttack();
                        }
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
    protected virtual async void HandleAttack()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (_navigationAgent.DistanceToTarget() <= _attackDistance && !_isAttacking)
        {
            _isAttacking = true;
            GD.Print("Attacking player");
            _sprite.Play("attack");
            await Task.Delay(TimeSpan.FromMilliseconds(700));
            if (WeakRef(this) == null) { return; }
            ;
            if (_navigationAgent.DistanceToTarget() <= _attackDistance)
            {
                GlobalEvents.SendOnAttack(new AgentEventArgs { agent = this, damageAmount = _attackDamage });
                GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = "Hurt" });
            }
            else
            {
                GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = "PunchMiss" });
            }
            await Task.Delay(TimeSpan.FromMilliseconds(700));
            _isAttacking = false;
        }
    }
    protected override void PlayAnimation()
    {
        if (!_isAttacking)
        {
            base.PlayAnimation();
        }
        else
        {
            if (Velocity.X > 0)
            {
                _sprite.FlipH = true;
            }
            else
            {
                _sprite.FlipH = false;
            }
        }
    }

}
public static partial class GlobalEvents
{
    public delegate void AgentEvent(AgentEventArgs eventArgs);

    public static event AgentEvent OnAttack;
    public static event AgentEvent OnDamaged;

    public static void SendOnAttack(AgentEventArgs eventArgs)
    {
        OnAttack?.Invoke(eventArgs);
    }
    public static void SendOnDamaged(AgentEventArgs eventArgs)
    {
        OnDamaged?.Invoke(eventArgs);
    }
}

// Other event types
public class AgentEventArgs : EventArgs
{
    public BaseNavigationAgent2D agent;
    public PlayerTopDown player;
    public int damageAmount;
    public int healthLeft;

}
