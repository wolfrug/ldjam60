using Godot;
using System;
using Godot.Collections;

[GlobalClass, Tool]
public partial class InkAudioData : InkResource
{
    [Export] public Dictionary<string, AudioStream> audioStreams = new();
    public bool TryGetAudioStream(string id, out AudioStream stream) => audioStreams.TryGetValue(id, out stream);
}
