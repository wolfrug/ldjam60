using Godot;
using System;
using Godot.Collections;

public partial class Boot : Node
{
    [Export] public string _startScene;
    [Export] public Button _startButton;
    [Export] public Button _quitButton;

    public override void _Ready()
    {
        base._Ready();
        if (_startButton != null)
        {
            _startButton.Pressed += StartGame;
        }
        if (_quitButton != null)
        {
            _quitButton.Pressed += QuitGame;
        }
    }

    public void StartGame()
    {
        GetTree().ChangeSceneToFile(_startScene);
    }
    public void QuitGame()
    {
        GetTree().Quit();
    }

}
