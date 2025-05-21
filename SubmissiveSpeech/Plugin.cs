using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SubmissiveSpeech.Windows;
using System.Net.NetworkInformation;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using RestrictedSpeech;
using System;
using Dalamud.Utility.Signatures;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Utility;
using Dalamud.Memory;
using System.Text.RegularExpressions;
using System.Linq;

namespace SubmissiveSpeech;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider InteropProvider { get; private set; } = null!;
    private const string ConfigCommandName = "/subspeech";

    private RestrictedSpeech.SubmissiveSpeech speech = null!;
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Sub Speech");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private unsafe delegate byte ProcessChatInputDelegate(IntPtr uiModule, byte** message, IntPtr a3);
    [Signature("E8 ?? ?? ?? ?? FE 86 ?? ?? ?? ?? C7 86 ?? ?? ?? ?? ?? ?? ?? ??", DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto)]
    private Hook<ProcessChatInputDelegate> ProcessChatInputHook { get; set; } = null!;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        _configureCommands();
        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        speech = new RestrictedSpeech.SubmissiveSpeech(Configuration);// Speaker(Configuration);
        
        InteropProvider.InitializeFromAttributes(this);
        ProcessChatInputHook.Enable();
        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SubmissiveSpeech] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    private void _configureCommands() {

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles Forced Speech"
        });
    }
    
    private unsafe byte ProcessChatInputDetour(IntPtr uiModule, byte** message, IntPtr a3) {
         // Put all this shit in a try-catch loop so we can catch any possible thrown exception.
        try
        {
            // Grab the original string.
            var originalSeString = MemoryHelper.ReadSeStringNullTerminated((nint)(*message));
            var messageDecoded = originalSeString.ToString(); // the decoded message format.

            // Debug the output (remove later)
            foreach (var payload in originalSeString.Payloads)
                Log.Debug($"Message Payload [{payload.Type}]: {payload.ToString()}");

            if (string.IsNullOrWhiteSpace(messageDecoded))
            {
                Log.Debug("Message was null or whitespace, returning original.");
                return ProcessChatInputHook.Original(uiModule, message, a3);
            }

            // Create the new string to send.
            var newSeStringBuilder = new SeStringBuilder();

            var matchedCommand = "";
            var matchedChannelType = "";
            // At this point, make sure that the message is not a command, if it is we should ignore it.
            Log.Debug($"Detouring Message: {messageDecoded}");
            if (messageDecoded.StartsWith("/"))
            {
                // This means its not a chat channel command and just a normal command, so return original.
                if (matchedCommand.IsNullOrEmpty())
                {
                    Log.Debug("Ignoring Message as it is a command");
                    return ProcessChatInputHook.Original(uiModule, message, a3);
                }

                // Set the matched command to the matched channel type. 
                matchedChannelType = matchedCommand;

                // if tell command is matched, need extra step to protect target name
                if (matchedCommand.StartsWith("/tell") || matchedCommand.StartsWith("/t"))
                {
                    Log.Debug($"[Chat Processor]: Matched Command is a tell command");
                    /// Using /gag command on yourself sends /tell which should be caught by this
                    /// Depends on <seealso cref="MsgEncoder.MessageEncoder"/> message to start like :"/tell {targetPlayer} *{playerPayload.PlayerName}"
                    /// Since only outgoing tells are affected, {targetPlayer} and {playerPayload.PlayerName} will be the same
                    var selfTellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?<name>\S+\s{1}\S+)@\S+\s{1}\*\k<name>(?=\s|$)";
                    if (!Regex.Match(messageDecoded, selfTellRegex).Value.IsNullOrEmpty())
                    {
                        Log.Debug("[Chat Processor]: Ignoring Message as it is a self tell garbled message.");
                        return ProcessChatInputHook.Original(uiModule, message, a3);
                    }
                    // Match any other outgoing tell to preserve target name
                    var tellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?:\S+\s{1}\S+@\S+|\<r\>)\s?(?=\S|\s|$)";
                    matchedCommand = Regex.Match(messageDecoded, tellRegex).Value;
                }
                Log.Debug($"Matched Command [{matchedCommand}] for matchedChannelType: [{matchedChannelType}]");
            }

            // If current channel message is being sent to is in list of enabled channels, translate it.
            // only obtain the text payloads from this message, as nothing else should madder.
            var textPayloads = originalSeString.Payloads.OfType<TextPayload>().ToList();

            // merge together the text of all the split text payloads.
            var originalText = string.Join("", textPayloads.Select(tp => tp.Text));

            // after we have done that, take this string and get the substring with the matched command length.
            var stringToProcess = originalText.Substring(matchedCommand.Length);

            stringToProcess = speech.SubmissivelySpeak(stringToProcess);
            // if (Configuration.ForcedSpeechEnabled) {
            //     stringToProcess = speech.ForcedSpeech(stringToProcess);
            // }
            // if (Configuration.LockedSpeech) {
            //     stringToProcess = speech.LockedSpeech(stringToProcess);
            // }
            // if (Configuration.UtterancesEnabled) {
            //     stringToProcess = speech.Utterances(stringToProcess);
            // }

            // once we have done that, garble that string, and then merge it back with the output command in front.
            var output = matchedCommand + stringToProcess;

            // append this to the newSeStringBuilder.
            newSeStringBuilder.Add(new TextPayload(output));

            // DEBUG MESSAGE: (Remove when not debugging)
            Log.Debug("Output: " + output);

            if (string.IsNullOrWhiteSpace(output))
                return 0; // Do not sent message.

            // Construct it for finalization.
            var newSeString = newSeStringBuilder.Build();

            // Verify its a legal width
            if (newSeString.TextValue.Length <= 500)
            {
                var utf8String = Utf8String.FromString(".");
                utf8String->SetString(newSeString.Encode());
                return ProcessChatInputHook.Original(uiModule, (byte**)((nint)utf8String).ToPointer(), a3);
            }
            else // return original if invalid.
            {
                Log.Error("Chat Garbler Variant of Message was longer than max message length!");
                return ProcessChatInputHook.Original(uiModule, message, a3);
            }
        }
        catch (Exception e)
        { // cant ever have enough safety!
            Log.Error($"Error sending message to chat box (secondary): {e}");
        }
        // return the original message untranslated
        return ProcessChatInputHook.Original(uiModule, message, a3);
    }


    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        ProcessChatInputHook?.Disable();
        ProcessChatInputHook?.Dispose();
        CommandManager.RemoveHandler(ConfigCommandName);
    }

    private void OnCommand(string command, string args) {
        ConfigWindow.Toggle();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
