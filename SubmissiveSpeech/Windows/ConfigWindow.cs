using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog.Debugging;

namespace SubmissiveSpeech.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration;
    private bool previewStutterEnabled = false;
    private int previewStutterChance = 0;
    private int previewStutterSeverity = 0;

    private string previewForcedWords = "";
    private string previewTicks = "";
    private int previewTickChance = 0;
    private int previewMaxTicks = 0;

    private string previewSentenceStarts = "";
    private string previewSentenceEndings = "";
    private Dictionary<string, string> previewPronounReplacements = new Dictionary<string, string>() { };
    private string previewNewPronoun = "";
    private string previewNewReplacement = "";

    public bool previewForcedPronounEnabled = false;

    // public Dictionary<string, string> PronounsReplacements = new Dictionary<string, string>() { };

    private bool configDirty = false;
    private readonly CancellationTokenSource cts = new();

    private void GetLastConfig()
    {
        previewStutterEnabled = configuration.StutterEnabled;
        previewStutterChance = configuration.StutterChance;
        previewStutterSeverity = configuration.MaxStutterSeverity;

        previewForcedWords = string.Join(' ', configuration.ForcedWords);
        previewTicks = string.Join(' ', configuration.Utterances);
        previewTickChance = (int)(configuration.UtteranceChance * 100f);
        previewMaxTicks = (int)(configuration.UtteranceMaxPortionOfSpeech * 100f);

        previewSentenceStarts = configuration.SentenceStarts.Trim();
        previewSentenceEndings = configuration.SentenceEndings.Trim();

        previewTickChance = (int)(configuration.UtteranceChance * 100f);
        previewMaxTicks = (int)(configuration.UtteranceMaxPortionOfSpeech * 100f);

        previewForcedPronounEnabled = configuration.ForcedPronounsEnabled;
    }

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Subspeech Configuration###With a constant ID")
    {
        // Flags = ImGuiWindowFlags.AlwaysAutoResize;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
        GetLastConfig();

        _ = Task.Run((Func<Task?>)(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (configDirty)
                    {
                        this.configuration.Save();
                        configDirty = false;
                    }
                    await Task.Delay(2000, cts.Token); // Wait for 2 seconds before checking again
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }), cts.Token);
    }

    public void Dispose() { }

    private void InitTextFields() {

    }

    public override void Draw()
    {
        // var SubmissiveSpeech = Configuration.LockedSpeech;
        ImGui.Checkbox("Stutter Speech Enabled", ref previewStutterEnabled);
        if (ImGui.InputInt("Stutter Chance", ref previewStutterChance, 1, 10)) {
            Math.Clamp(previewStutterChance, 1, 100);
        }
        if (ImGui.InputInt("Max Stutter Severity", ref previewStutterSeverity, 1, 10)) {
            Math.Clamp(previewStutterSeverity, 1, 100);
        }
        var width = ImGui.GetWindowWidth() - ImGui.GetCursorPosX() - ImGui.GetStyle().WindowPadding.X;
        var ForcedSpeech = configuration.ForcedSpeechEnabled;
        if (ImGui.Checkbox("Forced Speech Enabled", ref ForcedSpeech))
        {
            configuration.ForcedSpeechEnabled = ForcedSpeech;
        }
        ImGui.InputTextMultiline("Forced Speech Word List", ref previewForcedWords, 4000, new Vector2(width, 40));
        ImGui.Separator();

        ImGui.Checkbox("Forced Pronouns", ref previewForcedPronounEnabled);

        if (ImGui.BeginTable("pronoun_table", 3, ImGuiTableFlags.SizingStretchSame)) {
            ImGui.TableSetupColumn("Pronoun", ImGuiTableColumnFlags.NoHide);
            ImGui.TableSetupColumn("What to Replace With", ImGuiTableColumnFlags.NoHide);
            ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.NoHide);
            ImGui.TableHeadersRow();

            foreach ((string k, string v) in configuration.PronounsReplacements)
            {
                var old_value = v;
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(k);
                ImGui.TableNextColumn();
                if (ImGui.InputText($"##{k}", ref old_value, 20))
                {
                    configuration.PronounsReplacements[k] = old_value;
                }
                ImGui.TableNextColumn();
                if (ImGui.Button($"x##delete{k}")) {
                    configuration.PronounsReplacements.Remove(k);
                }
            }

           
            ImGui.TableNextColumn();
            ImGui.InputText("##inputkey", ref previewNewPronoun, 20);
            ImGui.TableNextColumn();
            ImGui.InputText("##inputvalue", ref previewNewReplacement, 20);
            ImGui.TableNextColumn();
            if (ImGui.Button("+##new"))
            {
                configuration.PronounsReplacements.Add(previewNewPronoun, previewNewReplacement);
                previewNewPronoun = "";
                previewNewReplacement = "";
            }
            ImGui.EndTable();
        }

        ImGui.Separator();

        var SentenceStartEnabled = configuration.SentenceStartEnabled;
        if (ImGui.Checkbox("Sentence Starts Enabled", ref SentenceStartEnabled))
        {
            configuration.SentenceStartEnabled = SentenceStartEnabled;
        }
        ImGui.InputTextMultiline("Sentence Starts Word List", ref previewSentenceStarts, 4000, new Vector2(width, 40));
        ImGui.Separator();

        var SentenceEndingEnabled = configuration.SentenceEndingEnabled;
        if (ImGui.Checkbox("Sentence Endings Enabled", ref SentenceEndingEnabled))
        {
            configuration.SentenceEndingEnabled = SentenceEndingEnabled;
        }
        ImGui.InputTextMultiline("Sentence Endings Word List", ref previewSentenceEndings, 4000, new Vector2(width, 40));


        ImGui.Separator();
        var Utterances = configuration.UtterancesEnabled;
        if (ImGui.Checkbox("Verbal Ticks Enabled", ref Utterances))
        {
            if (configuration.Utterances.Count > 0)
            {
                configuration.UtterancesEnabled = Utterances;
            }
            else
            {

            }
        }


        if(ImGui.InputInt("Chance for a Tick", ref previewTickChance, 1, 10, ImGuiInputTextFlags.None) ) {
            Math.Clamp(previewTickChance, 0, 100);
        }
        if(ImGui.InputInt("Max % of Speech that can be a tick", ref previewMaxTicks, 1, 10, ImGuiInputTextFlags.None) ) {
            Math.Clamp(previewMaxTicks, 0, 100);
        }

        ImGui.InputTextMultiline("Utterances", ref previewTicks, 4000, new Vector2(width, 40));
        
        ImGui.Separator();
        if (ImGui.Button("Save Config"))
        {
            configuration.ForcedWords = previewForcedWords.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            configuration.Utterances = previewTicks.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            
            configuration.UtteranceChance = (float)(previewTickChance / 100f);
            configuration.UtteranceMaxPortionOfSpeech = (float)(previewMaxTicks / 100f);

            configuration.SentenceStarts = previewSentenceStarts.Trim();
            configuration.SentenceEndings = previewSentenceEndings.Trim();

            configuration.StutterEnabled = previewStutterEnabled;
            configuration.StutterChance = previewStutterChance;
            configuration.MaxStutterSeverity = previewStutterSeverity;

            configuration.ForcedPronounsEnabled = previewForcedPronounEnabled;

            configDirty = true;
        }
        if (ImGui.Button("Revert"))
        {
            configuration = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            GetLastConfig();
        }
    }
}
