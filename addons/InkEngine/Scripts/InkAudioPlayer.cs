using Godot;
using InkEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

public partial class InkAudioPlayer : AudioStreamPlayer
{
    public static InkAudioPlayer instance;
    private const string c_playMusicTag = "PLAY_MUSIC:";
    private const string c_playSFXTag = "PLAY_SFX:";
    private const string c_playAmbientTag = "PLAY_AMBIENCE:";
    private const char c_delimiter = '^';

    private List<AudioStreamPlayer> audioPlayerPool = new List<AudioStreamPlayer> { };

    [Export] private AudioStreamPlayer ambiencePlayer;
    private Action ambienceLoopAction;
    [Export] private AudioStreamPlayer musicPlayer;
    private Action musicLoopAction;

    [Export] private InkAudioData defaultAudioData;

    public override void _EnterTree()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            QueueFree();
            return;
        }
        GlobalEvents.OnTagsFound += GlobalEvent_OnTagsFound;
        GlobalEvents.OnPlaySFX += PlaySFX;
        GlobalEvents.OnPlayAmbience += PlayAmbience;
        GlobalEvents.OnPlayMusic += PlayMusic;
        base._EnterTree();
    }
    public override void _ExitTree()
    {
        if (instance == this)
        {
            instance = null;
        }
        GlobalEvents.OnTagsFound -= GlobalEvent_OnTagsFound;
        GlobalEvents.OnPlaySFX -= PlaySFX;
        GlobalEvents.OnPlayAmbience -= PlayAmbience;
        GlobalEvents.OnPlayMusic -= PlayMusic;
        base._ExitTree();
    }
    private void GlobalEvent_OnTagsFound(InkEventArgs args)
    {
        foreach (string tag in args.inkTags)
        {
            ParseAudioStream(tag);
        }
    }
    private void ParseAudioStream(string tag)
    {
        if (tag.Contains(c_playMusicTag))
        {
            PlayMusicParse(tag);
        }
        if (tag.Contains(c_playAmbientTag))
        {
            PlayAmbienceParse(tag);
        }
        if (tag.Contains(c_playSFXTag))
        {
            PlaySFXParse(tag);
        }
    }

    private void PlayMusicParse(string tag)
    {
        //GD.Print("Looking to play music at location " + tag);
        tag = tag.Replace(c_playMusicTag, "");
        string[] stringParts = tag.Split(c_delimiter);
        string filePath = stringParts[0];
        AudioStream stream = LoadAudioStream(filePath);
        GlobalEvents.SendOnPlayMusic(new InkAudioEventArgs
        {
            targetPlayer = this,
            audioStream = stream,
            audioPath = filePath,
            looping = true
        });
    }
    public void PlayMusic(InkAudioEventArgs args)
    {
        //GD.Print("Looking to play music at location " + tag);
        string filePath = args.audioPath;
        bool looping = args.looping;
        AudioStream stream = args.audioStream;
        if (musicPlayer == null)
        {
            musicPlayer = new AudioStreamPlayer();
            AddChild(musicPlayer);
        }
        if (stream == null)
        {
            stream = LoadAudioStream(filePath);
        }
        if (stream != null && (musicPlayer.Stream != stream || !musicPlayer.Playing))
        {
            PlayAudioStream(stream, musicPlayer, musicLoopAction, looping);
        }
        else if (stream == null && musicPlayer.Playing)
        {
            musicPlayer.Stop();
        }
    }

    private void PlaySFXParse(string tag)
    {
        //GD.Print("Looking to play sfx at location " + tag);
        tag = tag.Replace(c_playSFXTag, "");
        string[] stringParts = tag.Split(c_delimiter);
        string filePath = stringParts[0];
        AudioStream stream = LoadAudioStream(filePath);
        GlobalEvents.SendOnPlaySFX(new InkAudioEventArgs
        {
            targetPlayer = this,
            audioPath = filePath,
            audioStream = stream,
            looping = false
        });
    }

    public void PlaySFX(InkAudioEventArgs args)
    {
        //GD.Print("Looking to play sfx at location " + tag);
        string filePath = args.audioPath;
        AudioStream stream = args.audioStream;
        if (stream == null)
        {
            stream = LoadAudioStream(filePath);
        }
        if (stream != null)
        {
            if (audioPlayerPool.Count > 0)
            {
                audioPlayerPool[0].Stream = stream;
                audioPlayerPool[0].Play();
                audioPlayerPool.RemoveAt(0);
            }
            else
            {
                AudioStreamPlayer newPlayer = new AudioStreamPlayer();
                Action finishedAction = () => audioPlayerPool.Add(newPlayer);
                AddChild(newPlayer);
                newPlayer.Finished += finishedAction;
                newPlayer.Stream = stream;
                newPlayer.Play();
            }
        }
    }
    private void PlayAmbienceParse(string tag)
    {
        //GD.Print("Looking to play ambience at location " + tag);
        tag = tag.Replace(c_playAmbientTag, "");
        string[] stringParts = tag.Split(c_delimiter);
        string filePath = stringParts[0];
        AudioStream stream = LoadAudioStream(filePath);
        GlobalEvents.SendOnPlayAmbience(new InkAudioEventArgs
        {
            targetPlayer = this,
            audioPath = filePath,
            audioStream = stream,
            looping = true
        });
    }

    public void PlayAmbience(InkAudioEventArgs args)
    {
        //GD.Print("Looking to play ambience at location " + tag);
        string filePath = args.audioPath;
        bool looping = args.looping;
        AudioStream stream = args.audioStream;
        if (ambiencePlayer == null)
        {
            ambiencePlayer = new AudioStreamPlayer();
            AddChild(ambiencePlayer);
        }
        if (stream == null)
        {
            stream = LoadAudioStream(filePath);
        }
        if (stream != null && (ambiencePlayer.Stream != stream || !ambiencePlayer.Playing))
        {
            PlayAudioStream(stream, ambiencePlayer, ambienceLoopAction, looping);
        }
        else if (stream == null && ambiencePlayer.Playing)
        {
            ambiencePlayer.Stop();
        }
    }

    private void PlayAudioStream(AudioStream stream, AudioStreamPlayer player, Action loopAction, bool isLooping = true)
    {

        player.Stream = stream;
        player.Play();
        if (loopAction != null)
        {
            player.Finished -= loopAction;
        }
        loopAction = () => player.Play();
        if (isLooping)
        {
            player.Finished += loopAction;
        }

    }

    public AudioStream LoadAudioStream(string filePath)
    {
        AudioStream loadedStream;
        if (defaultAudioData != null && defaultAudioData.TryGetAudioStream(filePath, out loadedStream))
        {
            return loadedStream;
        }
        filePath = "res://" + filePath;
        if (ResourceLoader.Exists(filePath))
        {
            loadedStream = ResourceLoader.Load(filePath) as AudioStream;
            return loadedStream;
        }
        else
        {
            GD.Print("No Audio Stream found at " + filePath);
            return null;
        }
    }
}
public static partial class GlobalEvents
{
    // UI Events
    public delegate void InkAudioEvent(InkAudioEventArgs eventArgs);

    public static event InkAudioEvent OnPlaySFX;
    public static event InkAudioEvent OnPlayAmbience;
    public static event InkAudioEvent OnPlayMusic;

    public static void SendOnPlaySFX(InkAudioEventArgs eventArgs)
    {
        OnPlaySFX?.Invoke(eventArgs);
    }
    public static void SendOnPlayAmbience(InkAudioEventArgs eventArgs)
    {
        OnPlayAmbience?.Invoke(eventArgs);
    }
    public static void SendOnPlayMusic(InkAudioEventArgs eventArgs)
    {
        OnPlayMusic?.Invoke(eventArgs);
    }

}
public class InkAudioEventArgs : EventArgs
{
    public InkAudioPlayer targetPlayer;
    public AudioStream audioStream;
    public string audioPath;
    public bool looping;
}
