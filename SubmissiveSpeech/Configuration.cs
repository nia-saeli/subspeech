using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using System;
using System.Collections.Generic;

namespace SubmissiveSpeech;

// TODO: Convert the configuration to include profiles with all the contained information.
// Ensure that the profiles 
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    // public Dictionary<string, Profile> DefaultProfile = new Dictionary<Guid, Profile>();
    // public Dictionary<Guid, Profile> CustomProfile = new Dictionary<Guid, Profile>();
    // public Profile CurrentProfile  = new Profile();

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public bool LockedSpeech { get; set; } = false;
    public bool ForcedSpeechEnabled { get; set; } = false;
    public bool UtterancesEnabled { get; set; } = false;
    public bool Locked { get; set; } = false;
    public bool ForcedPronounsEnabled { get; set; } = false;
    public bool StutterEnabled { get; set; } = false;
    public bool SentenceStartEnabled { get; set; } = false;
    public bool SentenceEndingEnabled { get; set; } = false;

    public HashSet<string> AllowedWords = new HashSet<string>() { };
    public List<string> ForcedWords = new List<string>() { };
    public List<string> Utterances = new List<string>() { };

    public string SentenceStarts = "";
    public string SentenceEndings = "";
    public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>();

    public float UtteranceChance = 1.00f;
    public float UtteranceMaxPortionOfSpeech = 0.1f;
    public string UtteranceEnd = "";
    public string CompelledSpeech = "";

    public int StutterChance = 10;
    public int MaxStutterSeverity = 3;
    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
// TODO: Convert the configuration to include profiles with all the contained information.
[Serializable]
public class Profile
{
    public string Label { get; set; } = "";
    public bool LockedSpeech { get; set; } = false;
    public bool ForcedSpeechEnabled { get; set; } = false;
    public bool UtterancesEnabled { get; set; } = false;
    public bool Locked { get; set; } = false;
    public bool ForcedPronounsEnabled { get; set; } = false;
    public bool StutterEnabled { get; set; } = false;
    public bool SentenceStartEnabled { get; set; } = false;
    public bool SentenceEndingEnabled { get; set; } = false;

    public HashSet<string> AllowedWords = new HashSet<string>() { };
    public List<string> ForcedWords = new List<string>() { };
    public List<string> Utterances = new List<string>() { };

    public string SentenceStarts = "";
    public string SentenceEndings = "";
    public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>();

    public float UtteranceChance = 1.00f;
    public float UtteranceMaxPortionOfSpeech = 0.1f;
    public string UtteranceEnd = "";
    public string CompelledSpeech = "";

    public int StutterChance = 10;
    public int MaxStutterSeverity = 3;
    public int MaxStuttersPerSentence = 3;
}