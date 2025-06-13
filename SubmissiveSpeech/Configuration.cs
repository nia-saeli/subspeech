using Dalamud.Configuration;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
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
    public Guid ActiveProfileId;
    public int ActiveProfileIndex
    {
        get
        {
            if (Profiles.Count == 0)
            {
                return -1;
            }
            else
                return Profiles.FindIndex(0, Profiles.Count, profile => profile.Id.Equals(ActiveProfileId));
        }
    }
    public List<Profile> Profiles = new List<Profile>();
    public Profile ActiveProfile
    {
        get
        {
            var profile = Profiles.Find(profile => profile.Id.Equals(ActiveProfileId));
            if (profile == null)
            {
                Plugin.Log.Debug($"ProfileID {ActiveProfileId} is null returning default");
                return Profiles[0];
            }
            else
            {
                Plugin.Log.Debug($"returning ProfileID {ActiveProfileId}");
                return profile;
            }
        }
    }

    public bool Locked { get; set; } = false;
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
    public static List<Profile> BuiltInProfiles()
    {
        List<Profile> profiles = new List<Profile>() {
            new Profile {
                Label = "Empty",
                Readonly = true,
            },
            new Profile {
                Label = "Submissive",
                Readonly = true,
                CompelledSpeechEnabled = false,
                TicksEnabled = false,
                PronounCorrectionEnabled = false,
                StutterEnabled = false,
                SentenceStartEnabled = false,
                SentenceEndingEnabled = false,
                LowercaseEnforcement = true,
                CompelledSpeechWords = new List<string>() {},
                Ticks = new List<string>() {},

                SentenceStarts = "",
                SentenceEndings = "",

                PronounsReplacements = new Dictionary<string, string>() {
                    { "i", "i" },
                    { "me", "me" },
                    { "myself", "myself" },
                    { "master", "Master" },
                    { "mistress", "Mistress" },
                    { "sir", "Sir" },
                    { "miss", "Miss" }
                },

                TickChance = 1.00f,
                TickMaxPortionOfSpeech = 0.1f,
                TickEnd = "",

                StutterChance = 10,
                MaxStutterSeverity = 3,
                MaxStuttersPerSentence = 3,
            },
            new Profile {
                Label = "Puppy",
                Readonly = true,
                TicksEnabled = true,
                PronounCorrectionEnabled = true,
                StutterEnabled = true,
                SentenceStartEnabled = false,
                SentenceEndingEnabled = false,
                CompelledSpeechWords = new List<string>() {},
                Ticks = new List<string>() {
                    "woof", "bark"
                },
                PronounsReplacements = new Dictionary<string, string>() {
                    { "i", "puppy" },
                    { "me", "puppy" },
                    { "i'm", "puppy is"},
                    { "im", "puppy is" },
                    { "myself", "this puppy" },
                    { "master", "Master" },
                    { "mistress", "Mistress" },
                    { "sir", "Sir" },
                    { "miss", "Miss" },
                    { "owner", "Owner" }
                },

                TickChance = 0.20f,
                TickMaxPortionOfSpeech = 0.2f,

                StutterChance = 10,
                MaxStutterSeverity = 2,
                MaxStuttersPerSentence = 3,
            },

            new Profile {
                Label = "Bimbo",
                Readonly = true,
                TicksEnabled = true,
                Ticks = new List<string>() {
                    "umm", "yah", "ya-know", "like", "lolz", "lyk", "totally", "omg",
                    "O-M-G", "ferreal", "ummmm", "like-stuff", "*giggles*"
                },
                SentenceEndings = "♥",
                TickChance = 0.3f,
                TickMaxPortionOfSpeech = 0.2f,
            },
            new Profile {
                Label = "Budget Gagspeak",
                Readonly = true,
                CompelledSpeechEnabled = true,
                CompelledSpeechWords = new List<string>() {
                    "Mmmph!", "Mrrph!!", "Mmrph!", "Mmmmmm"
                }
            },
        };
        return profiles;
    }

    internal void SetActiveProfile(Guid id)
    {
        Plugin.Log.Debug($"Attempting to set {id}");
        var p = Profiles.Find(v => v.Id.Equals(id));
        // if (0 >= Profiles.FindIndex(0, Profiles.Count, v => v.Id.Equals(id)))
        if (p != null)
        {
            this.ActiveProfileId = id;
            Plugin.Log.Debug($"Found and setting {this.ActiveProfileId} as ActiveProfileId");
        }
    }

    public Configuration()
    {
        if (this.Profiles.Count > 0)
            this.ActiveProfileId = this.Profiles[0].Id;
    }
}
// TODO: Convert the configuration to include profiles with all the contained information.
[Serializable]
public class Profile
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public bool Readonly { get; set; } = false;
    public bool CompelledSpeechEnabled { get; set; } = false;
    public bool TicksEnabled { get; set; } = false;
    public bool PronounCorrectionEnabled { get; set; } = false;
    public bool StutterEnabled { get; set; } = false;
    public bool SentenceStartEnabled { get; set; } = false;
    public bool SentenceEndingEnabled { get; set; } = false;
    public bool LowercaseEnforcement { get; set; } = false;

    public List<string> CompelledSpeechWords = new List<string>();
    public List<string> Ticks = new List<string>();

    public string SentenceStarts = "";
    public string SentenceEndings = "";

    public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>() { };

    public float TickChance = 1.00f;
    public float TickMaxPortionOfSpeech = 0.1f;
    public string TickEnd = "";

    public int StutterChance = 10;
    public int MaxStutterSeverity = 3;
    public int MaxStuttersPerSentence = 3;

    public Profile()
    {
        this.Id = Guid.NewGuid();
    }
}
class ProfileEditor
{
    private string addPronounKey = "";
    private string addPronounValue = "";
    private string addTick = "";
    private string addCompelledWord = "";
    /// Returns if save has been pressed.
    public bool Draw(Profile profile)
    {
        if (ImGui.CollapsingHeader($"Profile##{profile.Id}"))
        {
            ImGui.BeginDisabled(profile.Readonly);
            if (profile.Readonly)
                ImGui.TextUnformatted("This is a built-in profile and cannot be changed. To customize your speech patters, create a custom profile from the main menu to change these settings.");
            InputText($"Label##{profile.Id}label", profile.Label, 64, v => profile.Label = v);
            ImGui.EndDisabled();
        }

        this.DrawTogglesRows(profile);
        this.DrawTicksRows(profile);
        this.DrawStutterRows(profile);
        this.DrawSentenceConfigRows(profile);
        if (!profile.Readonly && ImGui.Button("Save Changes"))
        {
            return true;
        }
        return false;
    }

    private void InputText(string label, string input, uint length, Action<string> setter)
    {
        var tmp = input;
        if (ImGui.InputText(label, ref tmp, length) && tmp != input)
        {
            setter(tmp);
        }

    }

    private void InputInt(string label, int current, Action<int> setter)
    {
        var tmp = current;
        if (ImGui.InputInt(label, ref tmp) && tmp != current)
        {
            setter(tmp);
        }
    }

    private void Checkbox(string label, bool current, Action<bool> setter)
    {
        var tmp = current;
        if (ImGui.Checkbox(label, ref tmp) && tmp != current)
        {
            setter(tmp);
        }
    }

    // Organizational Helpers to define specific portions of the UI Widgets.
    private void DrawWordListWidget(string name, List<string> words, ref string placeholderword)
    {
        using (ImRaii.Table($"{name}##{name}list", 2))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 48);
            ImGui.TableSetupColumn("Word");
            ImGui.TableHeadersRow();

            ImGui.TableNextColumn();
            if (ImGui.Button("+##add"))
            {
                words.Add(placeholderword);
                placeholderword = "";
            }
            ImGui.TableNextColumn();
            ImGui.InputText("##newword", ref placeholderword, 40);

            for (int i = 0; i < words.Count; i++)
            {
                ImGui.TableNextColumn();
                if (ImGui.Button("x"))
                {
                    words.RemoveAt(i);
                }
                ImGui.TableNextColumn();
                string tempWord = words[i];
                if (ImGui.InputText($"##{i}word", ref tempWord, 40))
                {
                    words[i] = tempWord;
                }
            }
        }
    }

    private void DrawDictionaryWidget(string name, Dictionary<string, string> dict, ref string placeholderKey, ref string placeholderValue)
    {
        using (ImRaii.Table($"{name}###{name}dictionary", 3))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 48);
            ImGui.TableSetupColumn("Noun (Not Case Sensitive)");
            ImGui.TableSetupColumn("Replacement (case matters)");
            ImGui.TableHeadersRow();

            foreach ((string k, string v) in dict)
            {
                ImGui.TableNextColumn();
                if (ImGui.Button($"x##delete{k}"))
                {
                    dict.Remove(k);
                }
                var old_value = v;
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(k);
                ImGui.TableNextColumn();
                InputText($"##{k}", dict[k], 20, v => dict[k] = v);
            }

            ImGui.TableNextColumn();
            if (ImGui.Button($"+##{name}new"))
            {
                dict.Add(placeholderKey, placeholderValue);
                placeholderKey = "";
                placeholderValue = "";
            }
            ImGui.TableNextColumn();
            ImGui.InputText($"##{name}inputkey", ref placeholderKey, 20);
            ImGui.TableNextColumn();
            ImGui.InputText($"##{name}inputvalue", ref placeholderValue, 20);
        }
    }

    private void DrawTogglesRows(Profile profile)
    {
        if (ImGui.CollapsingHeader($"Feature Toggles##{profile.Id}feature_toggles"))
        {
            ImGui.BeginDisabled(profile.Readonly);
            Checkbox("Compelled Speech Enabled", profile.CompelledSpeechEnabled, v => profile.CompelledSpeechEnabled = v);
            Checkbox("TicksEnabled", profile.TicksEnabled, v => profile.TicksEnabled = v);
            Checkbox("PronounCorrectionEnabled", profile.PronounCorrectionEnabled, v => profile.PronounCorrectionEnabled = v);
            Checkbox("StutterEnabled", profile.StutterEnabled, v => profile.StutterEnabled = v);
            Checkbox("SentenceStartEnabled", profile.SentenceStartEnabled, v => profile.SentenceStartEnabled = v);
            Checkbox("SentenceEndingEnabled", profile.SentenceEndingEnabled, v => profile.SentenceEndingEnabled = v);
            ImGui.EndDisabled();
        }
    }

    private void DrawTicksRows(Profile profile)
    {
        if (ImGui.CollapsingHeader($"Vocal Ticks##{profile.Id}ticks"))
        {
            ImGui.BeginDisabled(profile.Readonly);
            this.DrawWordListWidget("Possible Ticks", profile.Ticks, ref addTick);
            int tempTickChance = (int)(profile.TickChance * 100);
            InputInt("Tick Chance %", (int)(profile.TickChance * 100), v =>
                profile.TickChance = (float)(v / 100.0));
            int tempTickMaxPortionOfSpeech = (int)(profile.TickMaxPortionOfSpeech * 100);
            InputInt("Tick Portion of Speech", (int)(profile.TickMaxPortionOfSpeech * 100), v =>
                profile.TickMaxPortionOfSpeech = (float)(v / 100.0));
            ImGui.EndDisabled();
        }
    }

    private void DrawStutterRows(Profile profile)
    {
        if (ImGui.CollapsingHeader($"Stutter Configuration##{profile.Id}stutter"))
        {
            ImGui.BeginDisabled(profile.Readonly);
            InputInt("StutterChance", profile.StutterChance, v => profile.StutterChance = v);
            InputInt("MaxStutterSeverity", profile.MaxStutterSeverity, v => profile.MaxStutterSeverity = v);
            InputInt("MaxStuttersPerSentence", profile.MaxStuttersPerSentence, v => profile.MaxStuttersPerSentence = v);
            ImGui.EndDisabled();
        }
    }

    private void DrawSentenceConfigRows(Profile profile)
    {
        if (ImGui.CollapsingHeader($"General Sentence Configuration##{profile.Id}general"))
        {
            ImGui.BeginDisabled(profile.Readonly);
            InputText("SentenceStarts", profile.SentenceStarts, 120, v => profile.SentenceStarts = v);
            InputText("SentenceEndings", profile.SentenceEndings, 120, v => profile.SentenceEndings = v);
            this.DrawDictionaryWidget("pronouns", profile.PronounsReplacements, ref addPronounKey, ref addPronounValue);
            this.DrawWordListWidget("compelled words", profile.CompelledSpeechWords, ref addCompelledWord);
            ImGui.EndDisabled();
        }
    }
}
