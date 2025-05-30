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
    public Guid id { get; set; }
    public string Label { get; set; } = "";
    public bool Readonly { get; set; } = false;
    public bool ForcedSpeechEnabled { get; set; } = false;
    public bool TicksEnabled { get; set; } = false;
    public bool PronounCorrectionEnabled { get; set; } = false;
    public bool StutterEnabled { get; set; } = false;
    public bool SentenceStartEnabled { get; set; } = false;
    public bool SentenceEndingEnabled { get; set; } = false;

    public List<string> CompelledSpeechWords = new List<string>() { "..." };
    public List<string> Ticks = new List<string>() { "umm", "uh" };

    public string SentenceStarts = "";
    public string SentenceEndings = "";

    public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>() {
        { "i", "i" },
        { "me", "me" },
        { "myself", "myself" },
        { "master", "Master" },
        { "mistress", "Mistress" },
        { "sir", "Sir" },
        { "miss", "Miss" }
    };

    public float TickChance = 1.00f;
    public float TickMaxPortionOfSpeech = 0.1f;
    public string TickEnd = "";

    public int StutterChance = 10;
    public int MaxStutterSeverity = 3;
    public int MaxStuttersPerSentence = 3;

    public Profile()
    {
        this.id = Guid.NewGuid();
    }
}
class ProfileEditor
{

    private string addPronounKey = "";
    private string addPronounValue = "";
    private string addTick = "";
    private Profile profile;

    public ProfileEditor(Profile profile)
    {
        this.profile = profile;
    }
    public void Draw()
    {
        if (ImGui.CollapsingHeader($"Profile##{this.profile.id}"))
        {
            var tempLabel = this.profile.Label;
            if (ImGui.InputText($"Label##{this.profile.id}label", ref tempLabel, 64))
            {
                this.profile.Label = tempLabel;
            }
            // TODO: Make this conditionally editable
            var tempReadonly = this.profile.Readonly;
            if (ImGui.Checkbox($"Readonly##{this.profile.id}readonly", ref tempReadonly))
            {
                this.profile.Readonly = tempReadonly;
            }
        }

        this.DrawTogglesRows();
        this.DrawTicksRows();
        this.DrawStutterRows();
        this.DrawSentenceConfigRows();
    }
    // Organizational Helpers to define specific portions of the UI Widgets.
    private void DrawWordListWidget(string name, List<string> words, ref string placeholderword)
    {
        using (ImRaii.Table($"{name}##{this.profile.id}{name}", 2))
        {
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 48);
            ImGui.TableSetupColumn("Word");
            ImGui.TableHeadersRow();

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

            ImGui.TableNextColumn();
            if (ImGui.Button("+##add"))
            {
                words.Add(placeholderword);
                placeholderword = "";
            }

            ImGui.TableNextColumn();
            ImGui.InputText("##newword", ref placeholderword, 40);
        }
    }

    private void DrawDictionaryWidget(string name, Dictionary<string, string> dict, ref string placeholderKey, ref string placeholderValue)
    {
        using (ImRaii.Table($"{name}###{name}{this.profile.id}", 3))
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
                if (ImGui.InputText($"##{k}", ref old_value, 20))
                {
                    dict[k] = old_value;
                }
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
    private void DrawTogglesRows()
    {
        if (ImGui.CollapsingHeader($"Toggles##{this.profile.id}feature_toggles"))
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("ForcedSpeechEnabled");
            ImGui.TableNextColumn();
            var tempForcedSpeechEnabled = this.profile.ForcedSpeechEnabled;
            if (ImGui.Checkbox("", ref tempForcedSpeechEnabled))
            {
                this.profile.ForcedSpeechEnabled = tempForcedSpeechEnabled;
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("TicksEnabled");
            ImGui.TableNextColumn();
            var tempTicksEnabled = this.profile.TicksEnabled;
            if (ImGui.Checkbox("##TicksEnabled", ref tempTicksEnabled))
            {
                this.profile.TicksEnabled = tempTicksEnabled;
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("PronounCorrectionEnabled");
            ImGui.TableNextColumn();
            var tempPronounCorrectionEnabled = this.profile.PronounCorrectionEnabled;
            if (ImGui.Checkbox("##PronounCorrectionEnabled", ref tempPronounCorrectionEnabled))
            {
                this.profile.PronounCorrectionEnabled = tempPronounCorrectionEnabled;
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("StutterEnabled");
            ImGui.TableNextColumn();
            var tempStutterEnabled = this.profile.StutterEnabled;
            if (ImGui.Checkbox("##StutterEnabled", ref tempStutterEnabled))
            {
                this.profile.StutterEnabled = tempStutterEnabled;
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("SentenceStartEnabled");
            ImGui.TableNextColumn();
            var tempSentenceStartEnabled = this.profile.SentenceStartEnabled;
            if (ImGui.Checkbox("##SentenceStartEnabled", ref tempSentenceStartEnabled))
            {
                this.profile.SentenceStartEnabled = tempSentenceStartEnabled;
            }
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("SentenceEndingEnabled");
            ImGui.TableNextColumn();
            var tempSentenceEndingEnabled = this.profile.SentenceEndingEnabled;
            if (ImGui.Checkbox("##SentenceEndingEnabled", ref tempSentenceEndingEnabled))
            {
                this.profile.SentenceStartEnabled = tempSentenceEndingEnabled;
            }
        }
    }

    private void DrawTicksRows()
    {
        if (ImGui.CollapsingHeader($"Vocal Ticks##{this.profile.id}ticks"))
        {
            this.DrawWordListWidget("Possible Ticks", this.profile.Ticks, ref addTick);
            int tempTickChance = (int)(this.profile.TickChance * 100);
            if (ImGui.InputInt("Tick Chance %", ref tempTickChance, 1, 10))
            {
                this.profile.TickChance = (float)(tempTickChance / 100.0);
            }
            int tempTickMaxPortionOfSpeech = (int)(this.profile.TickMaxPortionOfSpeech * 100);
            if (ImGui.InputInt("Tick Portion of Speech", ref tempTickMaxPortionOfSpeech, 1, 10))
            {
                this.profile.TickMaxPortionOfSpeech = (float)(tempTickMaxPortionOfSpeech / 100.0);
            }
        }
    }

    private void DrawStutterRows()
    {
        if (ImGui.CollapsingHeader($"Stutter Configuration##{this.profile.id}stutter"))
        {
            ImGui.InputInt("StutterChance", ref this.profile.StutterChance, 1, 10);
            ImGui.InputInt("MaxStutterSeverity", ref this.profile.MaxStutterSeverity, 1, 10);
            ImGui.InputInt("MaxStuttersPerSentence", ref this.profile.MaxStuttersPerSentence, 1, 10);
        }
    }

    private void DrawSentenceConfigRows()
    {
        if (ImGui.CollapsingHeader($"General Sentence Configuration##{this.profile.id}general"))
        {
            ImGui.InputText("SentenceStarts", ref this.profile.SentenceStarts, 120);
            ImGui.InputText("SentenceEndings", ref this.profile.SentenceEndings, 120);
            this.DrawDictionaryWidget("pronouns", this.profile.PronounsReplacements, ref addPronounKey, ref addPronounValue);
        }
    }
}
