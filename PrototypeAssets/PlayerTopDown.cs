using Godot;

public partial class PlayerTopDown : CharacterBody2D
{
    [Export] public Camera2D camera;
    public const float Speed = 100.0f;

    public override void _Ready()
    {
        base._Ready();
        camera.MakeCurrent();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalEvents.OnLevelContinue += GlobalEvent_OnLevelContinue;
        GlobalEvents.OnLevelPaused += GlobalEvent_OnLevelPaused;
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GlobalEvents.OnLevelContinue -= GlobalEvent_OnLevelContinue;
        GlobalEvents.OnLevelPaused -= GlobalEvent_OnLevelPaused;
    }
    public void GlobalEvent_OnLevelPaused(LevelEventArgs args)
    {
        Active = false;
    }
    public void GlobalEvent_OnLevelContinue(LevelEventArgs args)
    {
        camera.MakeCurrent();
        Active = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Active)
        {
            Vector2 velocity = Velocity;
            Vector2 direction = Input.GetVector(
                "pan_left", "pan_right", "pan_up", "pan_down").Normalized();

            if (direction != Vector2.Zero)
            {
                velocity = direction * Speed;
                LookAt(Position + direction);
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
            }
            Velocity = velocity;
            MoveAndSlide();
        }
    }
    private bool _active = true;
    public bool Active
    {
        get
        {
            return _active;
        }
        set
        {
            _active = value;
        }
    }
}
