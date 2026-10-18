using System;
using Godot;
using Godot.Collections;

public partial class BaseNavigationAgent2D : CharacterBody2D
{
    [Export] private NavigationAgent2D _navigationAgent;
    [Export] public Node2D _navigationTarget;
    [Export] public Area2D _scanArea;
    [Export] public RayCast2D _rayCast;

    [Export] public Polygon2D _patrolRoute;
    protected Array<Vector2> _patrolRoutePoints = new();
    protected float _movementSpeed = 50.0f;
    protected Vector2 _movementTargetPosition;
    protected bool _isPatrolling = false;
    protected Array<Node2D> _bodiesInScanArea = new();

    protected int _currentPatrolPoint = -1;

    public Vector2 MovementTarget
    {
        get { return _navigationAgent.TargetPosition; }
        set { _navigationAgent.TargetPosition = value; }
    }

    public override void _Ready()
    {
        foreach (Vector2 point in _patrolRoute.Polygon)
        {
            _patrolRoutePoints.Add(ToGlobal(point));
            GD.Print("Added patrol route point " + ToGlobal(point));
        }
        _patrolRoute.Visible = false;
        if (_patrolRoutePoints.Count > 0)
        {
            _currentPatrolPoint = 0;
        }
        _scanArea.BodyEntered += OnBodyEnter;
        _scanArea.BodyExited += OnBodyExit;
        base._Ready();
        if (_navigationAgent == null)
        {
            _navigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        }

        // These values need to be adjusted for the actor's speed
        // and the navigation layout.
        _movementTargetPosition = GlobalPosition;
        _navigationAgent.PathDesiredDistance = 4.0f;
        _navigationAgent.TargetDesiredDistance = 4.0f;

        // Make sure to not await during _Ready.
        Callable.From(ActorSetup).CallDeferred();
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
        Active = true;
    }

    protected virtual void OnBodyEnter(Node2D node2D)
    {
        _bodiesInScanArea.Add(node2D);
    }
    protected virtual void OnBodyExit(Node2D node2D)
    {
        _bodiesInScanArea.Remove(node2D);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Active)
        {
            base._PhysicsProcess(delta);
            if (_bodiesInScanArea.Count > 0)
            {
                HandleScannedBodies();
            }

            Vector2 currentAgentPosition = GlobalTransform.Origin;
            Vector2 nextPathPosition = _navigationAgent.GetNextPathPosition();

            Velocity = currentAgentPosition.DirectionTo(nextPathPosition) * _movementSpeed;
            MoveAndSlide();
            ActorSetup();
        }
    }
    protected virtual void HandleScannedBodies()
    {

    }
    protected async void HandlePatrol()
    {
        if (!_isPatrolling)
        {
            int closestPoint = -1;
            float distance = float.PositiveInfinity;
            // Find closest point
            for (int i = 0; i < _patrolRoutePoints.Count; i++)
            {
                _navigationAgent.TargetPosition = _patrolRoutePoints[i];
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                if (_navigationAgent.DistanceToTarget() < distance)
                {
                    closestPoint = i;
                    distance = _navigationAgent.DistanceToTarget();
                }
            }
            _currentPatrolPoint = closestPoint;
            _isPatrolling = true;
            GD.Print("Found closest patrol point: " + closestPoint);
        }
        else
        {
            GD.Print("Patrolling to point " + _currentPatrolPoint);
            _movementTargetPosition = _patrolRoutePoints[_currentPatrolPoint];
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            if (_navigationAgent.IsNavigationFinished())
            {
                if (_currentPatrolPoint < _patrolRoutePoints.Count - 1)
                {
                    _currentPatrolPoint++;
                }
                else
                {
                    _currentPatrolPoint = 0;
                }
            }
        }
    }

    protected async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Now that the navigation map is no longer empty, set the movement target.
        if (_navigationTarget != null)
        {
            _isPatrolling = false;
            _movementTargetPosition = _navigationTarget.GlobalPosition;
        }
        else
        {
            HandlePatrol();
        }
        MovementTarget = _movementTargetPosition;
    }
    protected bool _active = true;
    public virtual bool Active
    {
        get
        {
            return _active;
        }
        set
        {
            _active = value;
            if (!_active)
            {
                _navigationAgent.ProcessMode = ProcessModeEnum.Disabled;
            }
            else
            {
                _navigationAgent.ProcessMode = ProcessModeEnum.Inherit;
            }
        }
    }
}