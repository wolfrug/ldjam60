using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class InkLiveTextExtension : RichTextEffect

{
    [Export] public Variant tag = "live";
    public override bool _ProcessCustomFX(CharFXTransform charFX)
    {
        if (charFX.Env.TryGetValue(tag, out Variant result))
        {
            charFX.Visible = false;
            return true;
        }
        ;
        return false;
    }
}
