using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Linq;

namespace SubmissiveSpeech.Windows;

public class MainWindow : Window, IDisposable
{
    private enum WhichMenu
    {
        Main, Profile
    }
    private Plugin plugin;
    private ProfileEditor profileEditor = new ProfileEditor();
    private WhichMenu menu = WhichMenu.Main;
    private string[] profiles;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Submissive Speech", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        profiles = plugin.Configuration.Profiles.Select(selector => selector.Label).ToArray();
        Plugin.Log.Debug($"profiles:{profiles.Length}");
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Do not use .Text() or any other formatted function like TextWrapped(), or SetTooltip().
        // These expect formatting parameter if any part of the text contains a "%", which we can't
        // provide through our bindings, leading to a Crash to Desktop.
        // Replacements can be found in the ImGuiHelpers Class
        // ImGui.TextUnformatted($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");
        using (ImRaii.TabBar("Profile Settings##main"))
        {
            if (ImGui.BeginTabItem("Main Settings##mainsettings"))
            {
                drawMain();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Edit Active Profile##profileEdit"))
            {
                if (profileEditor.Draw(plugin.Configuration.ActiveProfile))
                {
                    plugin.Configuration.Save();
                }
                ImGui.EndTabItem();
            }
        }
        // if (ImGui.Button("Show Settings"))
        // {
        //     plugin.ToggleConfigUI();
        // }
        //
        // ImGui.Spacing();
        //
        // // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // // ImRaii takes care of this after the scope ends.
        // // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        // using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        // {
        //     // Check if this child is drawing
        //     if (child.Success)
        //     {
        //         ImGuiHelpers.ScaledDummy(20.0f);
        //
        //         // Example for other services that Dalamud provides.
        //         // ClientState provides a wrapper filled with information about the local player object and client.
        //
        //         var localPlayer = Plugin.ClientState.LocalPlayer;
        //         if (localPlayer == null)
        //         {
        //             ImGui.TextUnformatted("Our local player is currently not loaded.");
        //             return;
        //         }
        //
        //         if (!localPlayer.ClassJob.IsValid)
        //         {
        //             ImGui.TextUnformatted("Our current job is currently not valid.");
        //             return;
        //         }
        //
        //         // ExtractText() should be the preferred method to read Lumina SeStrings,
        //         // as ToString does not provide the actual text values, instead gives an encoded macro string.
        //         ImGui.TextUnformatted($"Our current job is ({localPlayer.ClassJob.RowId}) \"{localPlayer.ClassJob.Value.Abbreviation.ExtractText()}\"");
        //
        //         // Example for quarrying Lumina directly, getting the name of our current area.
        //         var territoryId = Plugin.ClientState.TerritoryType;
        //         if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
        //         {
        //             ImGui.TextUnformatted($"We are currently in ({territoryId}) \"{territoryRow.PlaceName.Value.Name.ExtractText()}\"");
        //         }
        //         else
        //         {
        //             ImGui.TextUnformatted("Invalid territory.");
        //         }
        //     }
        // }
    }
    private void drawMain()
    {
        int current = plugin.Configuration.ActiveProfileIndex;
        // Maybe write out some welcome text.
        ImGui.TextWrapped("""
                Hello there cutie, if you have this installed, you are likely the most 
                adorable subbie that needs a little bit of help to be the best subbie you can be.
                To get started, select one of the prebuilt profiles below.
                If you want some customization, you can duplicate a profile and then edit it in the tab above.

                Many apologies for the messy UI. This will hopefully be temporary <3

                - Miss Nia
                """);
        // create new profile
        if (ImGui.Button("New Blank Profile"))
        {
            var newpf = new Profile();
            newpf.Label = "New Profile";
            plugin.Configuration.Profiles.Add(newpf);
            profiles = plugin.Configuration.Profiles.Select(selector => selector.Label).ToArray();
            plugin.Configuration.Save();
        }
        // duplicate current profile
        // profile selector
        var label = current >= 0 ? profiles[current] : "Select a profile";
        if (ImGui.BeginCombo("Profile##selector", label))
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                if (ImGui.Selectable(profiles[i], i == current))
                {
                    var id = plugin.Configuration.Profiles[i].Id;
                    plugin.Configuration.SetActiveProfile(id);
                    current = plugin.Configuration.ActiveProfileIndex;
                    plugin.Configuration.Save();
                }
            }
            ImGui.EndCombo();
        }
    }
}
