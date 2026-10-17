using Godot;
using System;
using InkEngine;
using System.Collections.Generic;

public partial class InkBubbleChain : BubbleChain
{
    [Export] public string ID;

    private const string c_createBubbleChainTag = "SET_BUBBLECHAIN:";
    private const char c_delimiter = '^';

    public static Dictionary<string, InkBubbleChain> allBubbleChains = new Dictionary<string, InkBubbleChain> { };

    public void Init(string setID, SpeechBubble firstBubble)
    {
        if (allBubbleChains.TryGetValue(setID, out InkBubbleChain chain))
        {
            chain.AddToChain(firstBubble);
            this.QueueFree();
            GD.PrintErr("InkBubbleChain: attempted to init a bubble chain that already exists with ID " + setID);
        }
        ID = setID;
        ClearBubbles();
        allBubbleChains.Add(ID, this);
        AddToChain(firstBubble);
    }
    public override void _EnterTree()
    {
        base._EnterTree();
        GlobalEvents.OnLabelCreated += GlobalEvent_OnLabelCreated;
    }
    public override void _ExitTree()
    {
        base._EnterTree();
        GlobalEvents.OnLabelCreated -= GlobalEvent_OnLabelCreated;
    }

    protected virtual void GlobalEvent_OnLabelCreated(InkEventArgs args)
    {
        foreach (string inktag in args.inkTags)
        {
            if (inktag.Contains(c_createBubbleChainTag))
            {
                AddToChainTag(inktag, args.inkTextLabel);
            }
            if (inktag.Contains("clear"))
            {
                ClearBubbles();
            }
        }
    }

    public void AddToChainTag(string tag, InkLabel label)
    {
        string cleanedTag = tag.Replace(c_createBubbleChainTag, "");
        string[] stringParts = cleanedTag.Split(c_delimiter);
        if (stringParts[0] == ID)
        {
            if (label is InkSpeechBubble)
            {
                AddToChain((label as InkSpeechBubble).speechBubble);
            }
        }
    }
    public void AddToChain(SpeechBubble bubble)
    {
        AddBubble(bubble);
        GlobalTarget = bubble.GlobalTarget;
    }

}
