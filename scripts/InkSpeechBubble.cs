using Godot;
using GodotInk;
using System;
using InkEngine;
using System.Collections.Generic;

public partial class InkSpeechBubble : InkLabel
{
    [Export] public SpeechBubble speechBubble;
    protected const string c_setSpeechBubbleTag = "SET_BUBBLEPOS:";
    public override void Init(string text, List<string> tags = default)
    {
        base.Init(text, tags);

        if (attachedTags != null && attachedTags.Count > 0)
        {
            foreach (string tag in attachedTags)
            {
                if (tag.Contains(c_setSpeechBubbleTag))
                {
                    InitSpeechBubble(tag);
                }
            }
        }
    }
    public virtual void InitSpeechBubble(string tag)
    { // #SET_BUBBLEPOS:face OR #SET_BUBBLEPOS:344^200 OR #SET_BUBBLEPOS:face^chain_1 (for making bubble chains)
        string cleanedTag = tag.Replace(c_setSpeechBubbleTag, "");
        string[] stringParts = cleanedTag.Split(c_delimiter);
        GD.Print("Received SET BUBBLE POSITION coords, with parts: " + cleanedTag);
        string[] coordinates = stringParts[0].Split(',');

        Vector2 coords = Vector2.Zero;
        if (coordinates.Length == 1)
        {
            coords = InkBackgroundSprite2D.instance.presetCoordinates[coordinates[0]];
        }
        else
        {
            coords = new Vector2(coordinates[0].ToFloat(), coordinates[1].ToFloat());
        }
        if (speechBubble == null)
        {
            speechBubble = new SpeechBubble();
            container.AddChild(speechBubble);
            speechBubble.ShowBehindParent = true;
        }
        speechBubble.GlobalTarget = coords;

        // Add bubble chain if necessary
        if (stringParts.Length > 1)
        {
            if (InkBubbleChain.allBubbleChains.TryGetValue(stringParts[1], out InkBubbleChain oldChain))
            {
                oldChain.AddToChain(speechBubble);
            }
            else
            {
                InkBubbleChain newChain = new InkBubbleChain();
                GetTree().Root.AddChild(newChain);
                newChain.Init(stringParts[1], speechBubble);
            }
        }
    }
}
