using Godot;

public partial class PlayerTopDown : CharacterBody2D
{
	[Export] public AnimatedSprite2D sprite;
	[Export] public Camera2D camera;

	public const int BaseHP = 3;
	public const float Speed = 100.0f;

	public int CurrentHP = 3;
	public bool _alive = true;
	private float _timeUntilFootstep = 0.35f;

	public override void _Ready()
	{
		base._Ready();
		CurrentHP = BaseHP;
		camera.MakeCurrent();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		GlobalEvents.OnLevelContinue += GlobalEvent_OnLevelContinue;
		GlobalEvents.OnLevelPaused += GlobalEvent_OnLevelPaused;
		GlobalEvents.OnAttack += GlobalEvent_OnAttack;
	}
	public override void _ExitTree()
	{
		base._ExitTree();
		GlobalEvents.OnLevelContinue -= GlobalEvent_OnLevelContinue;
		GlobalEvents.OnLevelPaused -= GlobalEvent_OnLevelPaused;
		GlobalEvents.OnAttack -= GlobalEvent_OnAttack;
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
	public void GlobalEvent_OnAttack(AgentEventArgs args)
	{
		TakeDamage(args.damageAmount);
	}

	public void PlayFootStep()
	{
		if (GetRealVelocity() != Vector2.Zero)
		{
			GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = "Footstep" });
		}
		_timeUntilFootstep = 0.35f;
	}

	Tween _tween;
	public void TakeDamage(int amount)
	{
		if (_alive)
		{
			CurrentHP -= amount;
			if (_tween != null)
			{
				_tween.Kill();
			}
			_tween = CreateTween();
			_tween.TweenProperty(GetNode(GetPath()), "modulate:a", 0f, 0.05f).SetTrans(Tween.TransitionType.Sine);
			//_tween.TweenCallback(Callable.From(() => SetTexture2D(tex)));
			_tween.TweenProperty(GetNode(GetPath()), "modulate:a", 1f, 0.05f).SetTrans(Tween.TransitionType.Sine);
			if (CurrentHP <= 0)
			{
				_alive = false;
				sprite.Play(GetDeathAnimationName(_prevDirection));
				GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs { audioPath = "Death" });
			}
			GlobalEvents.SendOnDamaged(new AgentEventArgs { player = this, healthLeft = CurrentHP, damageAmount = amount });
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Active && _alive)
		{
			Vector2 velocity = Velocity;
			Vector2 direction = Input.GetVector(
				"pan_left", "pan_right", "pan_up", "pan_down").Normalized();

			if (direction != Vector2.Zero)
			{
				velocity = direction * Speed;
				//LookAt(Position + direction);
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
				velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
			}
			Velocity = velocity;
			MoveAndSlide();
			sprite.Play(GetAnimationName(direction));
			_timeUntilFootstep -= (float)delta;
			if (_timeUntilFootstep <= 0f)
			{
				PlayFootStep();
			}
		}
	}
	Vector2 _prevDirection = Vector2.Zero;
	public virtual string GetAnimationName(Vector2 direction)
	{
		if (direction == Vector2.Zero)
		{
			return GetIdleAnimationName(_prevDirection);
		}
		//GD.Print(direction);
		_prevDirection = direction;

		if (direction.X < 0 && direction.Y < 0)
		{
			return "walk_left_up";
		}
		if (direction.X == 1 && direction.Y == 0)
		{
			return "walk_right_down";
		}
		if (direction.X == -1 && direction.Y == 0)
		{
			return "walk_left_down";
		}
		if (direction.X > 0)
		{
			if (direction.Y > 0)
			{
				return "walk_right_down";
			}
			else
			{
				return "walk_right_up";
			}
		}
		if (direction.X < 0)
		{
			if (direction.Y > 0)
			{
				return "walk_left_down";
			}
			else
			{
				return "walk_left_up";
			}
		}
		if (direction.X == 0 && direction.Y == -1)
		{
			return "walk_up";
		}
		if (direction.X == 0 && direction.Y == 1)
		{
			return "walk_down";
		}
		return "idle_down";
	}
	public virtual string GetIdleAnimationName(Vector2 direction)
	{

		if (direction.X < 0 && direction.Y < 0)
		{
			return "idle_left_up";
		}
		if (direction.X == 1 && direction.Y == 0)
		{
			return "idle_right_down";
		}
		if (direction.X == -1 && direction.Y == 0)
		{
			return "idle_left_down";
		}
		if (direction.X > 0)
		{
			if (direction.Y > 0)
			{
				return "idle_right_down";
			}
			else
			{
				return "idle_right_up";
			}
		}
		if (direction.X < 0)
		{
			if (direction.Y > 0)
			{
				return "idle_left_down";
			}
			else
			{
				return "idle_left_up";
			}
		}
		if (direction.X == 0 && direction.Y == -1)
		{
			return "idle_up";
		}
		if (direction.X == 0 && direction.Y == 1)
		{
			return "idle_down";
		}
		return "idle_down";
	}
	public virtual string GetDeathAnimationName(Vector2 direction)
	{

		if (direction.X < 0 && direction.Y < 0)
		{
			return "die_left";
		}
		if (direction.X == 1 && direction.Y == 0)
		{
			return "die_right";
		}
		if (direction.X == -1 && direction.Y == 0)
		{
			return "die_left";
		}
		if (direction.X > 0)
		{
			if (direction.Y > 0)
			{
				return "die_right";
			}
			else
			{
				return "die_right";
			}
		}
		if (direction.X < 0)
		{
			if (direction.Y > 0)
			{
				return "die_left";
			}
			else
			{
				return "die_left";
			}
		}
		if (direction.X == 0 && direction.Y == -1)
		{
			return "die_up";
		}
		if (direction.X == 0 && direction.Y == 1)
		{
			return "die_down";
		}
		return "die_down";
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
