using Dalamud.Configuration;
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
    public Profile CurrentProfile = new Profile();

    public bool Locked { get; set; } = false;
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
    // This speech profile is readonly and may not be edited when applied.
    public bool Readonly { get; set; } = false;
    // Forced speech replaces all speech with one of the words in the list.
    // TODO: Rename petspeech
    public bool ForcedSpeechEnabled { get; set; } = false;
    // TODO: Rename to verbal ticks.
    /// <summary>
    /// Verbal ticks are inserted into the sentence 
    /// </summary>
    public bool UtterancesEnabled { get; set; } = false;
    /// <summary>
    /// Will automatically correct any incorrect pronouns in the list.
    /// </summary>
    public bool PronounCorrectionEnabled { get; set; } = false;
    /// <summary>
    /// Adds a random and adorable stutter
    /// </summary>
    public bool StutterEnabled { get; set; } = false;
    public bool SentenceStartEnabled { get; set; } = false;
    public bool SentenceEndingEnabled { get; set; } = false;

    public List<string> ForcedWords = new List<string>() { "..." };
    public List<string> Utterances = new List<string>() { "umm", "uh" };

    public string SentenceStarts = "";
    public string SentenceEndings = "";

    public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>()
    {
        { "i", "i" },
        { "me", "me" },
        { "myself", "myself" },
        { "master", "Master" },
        { "mistress", "Mistress" },
        { "sir", "Sir" },
        { "miss", "Miss" }
    };

    public float UtteranceChance = 1.00f;
    public float UtteranceMaxPortionOfSpeech = 0.1f;
    public string UtteranceEnd = "";
    public string CompelledSpeech = "";

    public int StutterChance = 10;
    public int MaxStutterSeverity = 3;
    public int MaxStuttersPerSentence = 3;
}
