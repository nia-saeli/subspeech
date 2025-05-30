using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;

namespace SubmissiveSpeech.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration;
    private bool previewStutterEnabled = false;
    private int previewStutterChance = 0;
    private int previewStutterSeverity = 0;

    public bool previewForcedPronounEnabled = false;

    private bool configDirty = false;
    private readonly CancellationTokenSource cts = new();

    private void GetLastConfig()
    {
        previewStutterEnabled = configuration.CurrentProfile.StutterEnabled;
        previewStutterChance = configuration.CurrentProfile.StutterChance;
        previewStutterSeverity = configuration.CurrentProfile.MaxStutterSeverity;

        previewForcedPronounEnabled = configuration.CurrentProfile.PronounCorrectionEnabled;
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

    private void InitTextFields()
    {

    }

    public override void Draw() { }
    // {
    //     // var SubmissiveSpeech = Configuration.LockedSpeech;
    //     ImGui.Checkbox("Stutter Speech Enabled", ref previewStutterEnabled);
    //     if (ImGui.InputInt("Stutter Chance", ref previewStutterChance, 1, 10))
    //     {
    //         Math.Clamp(previewStutterChance, 1, 100);
    //     }
    //     if (ImGui.InputInt("Max Stutter Severity", ref previewStutterSeverity, 1, 10))
    //     {
    //         Math.Clamp(previewStutterSeverity, 1, 100);
    //     }
    //     var width = ImGui.GetWindowWidth() - ImGui.GetCursorPosX() - ImGui.GetStyle().WindowPadding.X;
    //     var ForcedSpeech = configuration.CurrentProfile.ForcedSpeechEnabled;
    //     if (ImGui.Checkbox("Forced Speech Enabled", ref ForcedSpeech))
    //     {
    //         configuration.CurrentProfile.ForcedSpeechEnabled = ForcedSpeech;
    //     }
    //     ImGui.InputTextMultiline("Forced Speech Word List", ref previewForcedWords, 4000, new Vector2(width, 40));
    //     ImGui.Separator();
    //
    //     ImGui.Checkbox("Forced Pronouns", ref previewForcedPronounEnabled);
    //
    //     if (ImGui.BeginTable("pronoun_table", 3, ImGuiTableFlags.SizingStretchSame))
    //     {
    //         ImGui.TableSetupColumn("Pronoun", ImGuiTableColumnFlags.NoHide);
    //         ImGui.TableSetupColumn("What to Replace With", ImGuiTableColumnFlags.NoHide);
    //         ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.NoHide);
    //         ImGui.TableHeadersRow();
    //
    //         foreach ((string k, string v) in configuration.CurrentProfile.PronounsReplacements)
    //         {
    //             var old_value = v;
    //             ImGui.TableNextColumn();
    //             ImGui.TextUnformatted(k);
    //             ImGui.TableNextColumn();
    //             if (ImGui.InputText($"##{k}", ref old_value, 20))
    //             {
    //                 configuration.CurrentProfile.PronounsReplacements[k] = old_value;
    //             }
    //             ImGui.TableNextColumn();
    //             if (ImGui.Button($"x##delete{k}"))
    //             {
    //                 configuration.CurrentProfile.PronounsReplacements.Remove(k);
    //             }
    //         }
    //
    //         ImGui.TableNextColumn();
    //         ImGui.InputText("##inputkey", ref previewNewPronoun, 20);
    //         ImGui.TableNextColumn();
    //         ImGui.InputText("##inputvalue", ref previewNewReplacement, 20);
    //         ImGui.TableNextColumn();
    //         if (ImGui.Button("+##new"))
    //         {
    //             configuration.CurrentProfile.PronounsReplacements.Add(previewNewPronoun, previewNewReplacement);
    //             previewNewPronoun = "";
    //             previewNewReplacement = "";
    //         }
    //         ImGui.EndTable();
    //     }
    //
    //     ImGui.Separator();
    //
    //     var SentenceStartEnabled = configuration.CurrentProfile.SentenceStartEnabled;
    //     if (ImGui.Checkbox("Sentence Starts Enabled", ref SentenceStartEnabled))
    //     {
    //         configuration.CurrentProfile.SentenceStartEnabled = SentenceStartEnabled;
    //     }
    //     ImGui.InputTextMultiline("Sentence Starts Word List", ref previewSentenceStarts, 4000, new Vector2(width, 40));
    //     ImGui.Separator();
    //
    //     var SentenceEndingEnabled = configuration.CurrentProfile.SentenceEndingEnabled;
    //     if (ImGui.Checkbox("Sentence Endings Enabled", ref SentenceEndingEnabled))
    //     {
    //         configuration.CurrentProfile.SentenceEndingEnabled = SentenceEndingEnabled;
    //     }
    //     ImGui.InputTextMultiline("Sentence Endings Word List", ref previewSentenceEndings, 4000, new Vector2(width, 40));
    //
    //
    //     ImGui.Separator();
    //     var Utterances = configuration.CurrentProfile.UtterancesEnabled;
    //     if (ImGui.Checkbox("Verbal Ticks Enabled", ref Utterances))
    //     {
    //         if (configuration.CurrentProfile.Utterances.Count > 0)
    //         {
    //             configuration.CurrentProfile.UtterancesEnabled = Utterances;
    //         }
    //         else
    //         {
    //
    //         }
    //     }
    //
    //
    //     if (ImGui.InputInt("Chance for a Tick", ref previewTickChance, 1, 10, ImGuiInputTextFlags.None))
    //     {
    //         Math.Clamp(previewTickChance, 0, 100);
    //     }
    //     if (ImGui.InputInt("Max % of Speech that can be a tick", ref previewMaxTicks, 1, 10, ImGuiInputTextFlags.None))
    //     {
    //         Math.Clamp(previewMaxTicks, 0, 100);
    //     }
    //
    //     ImGui.InputTextMultiline("Utterances", ref previewTicks, 4000, new Vector2(width, 40));
    //
    //     ImGui.Separator();
    //     if (ImGui.Button("Save Config"))
    //     {
    //         configuration.CurrentProfile.CompelledSpeechWords = previewForcedWords.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    //         configuration.CurrentProfile.Utterances = previewTicks.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    //
    //         configuration.CurrentProfile.UtteranceChance = (float)(previewTickChance / 100f);
    //         configuration.CurrentProfile.UtteranceMaxPortionOfSpeech = (float)(previewMaxTicks / 100f);
    //
    //         configuration.CurrentProfile.SentenceStarts = previewSentenceStarts.Trim();
    //         configuration.CurrentProfile.SentenceEndings = previewSentenceEndings.Trim();
    //
    //         configuration.CurrentProfile.StutterEnabled = previewStutterEnabled;
    //         configuration.CurrentProfile.StutterChance = previewStutterChance;
    //         configuration.CurrentProfile.MaxStutterSeverity = previewStutterSeverity;
    //
    //         configuration.CurrentProfile.PronounCorrectionEnabled = previewForcedPronounEnabled;
    //
    //         configDirty = true;
    //     }
    //     if (ImGui.Button("Revert"))
    //     {
    //         configuration = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    //         GetLastConfig();
    //     }
    // }
}
