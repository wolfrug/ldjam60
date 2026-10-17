using Godot;
using GodotInk;
using System;
using System.Collections.Generic;
using InkEngine;

public static partial class GlobalEvents
{
    // UI Events
    public delegate void UIChangeEvent(UIEventArgs eventArgs);

    public static event UIChangeEvent OnUIShow;
    public static event UIChangeEvent OnUIHide;

    public static void SendOnUIShow(UIEventArgs eventArgs)
    {
        OnUIShow?.Invoke(eventArgs);
    }
    public static void SendOnUIHide(UIEventArgs eventArgs)
    {
        OnUIHide?.Invoke(eventArgs);
    }
}

// Other event types
public class UIEventArgs : EventArgs
{
    public Node targetNode;
}