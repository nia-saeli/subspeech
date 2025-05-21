using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Swift;
using System.Text;
using System.Text.RegularExpressions;
using Lumina.Excel.Sheets;
using Serilog;
using SubmissiveSpeech;

namespace RestrictedSpeech {
    public class SubmissiveSpeech
    {
        const string SENTENCE_REGEX = @"([^.]*[^.]*[\.\?\!])([^.]*[^.]*)$";
        const string WORD_REGEX = @"([^a-zA-Z0-9]*)([a-zA-Z0-9\-\']*)([^a-zA-Z0-9]*)";
// This emoji should support the following basic text emojis
        const string EMOJI_REGEX = @"^[:;cDxX><\-\,)CP][:;><\-3)CWwdpP]*[:;cDxX><\-\,3)CWwdpP]$";
        
        Configuration configuration;
        Regex sentence_regex;
        Regex word_regex;
        Regex emoji_regex;
        Regex compliance_regex;

        public SubmissiveSpeech(Configuration config)
        {
            configuration = config;
            sentence_regex = new Regex(SENTENCE_REGEX);
            word_regex = new Regex(WORD_REGEX);
            emoji_regex = new Regex(EMOJI_REGEX);
            compliance_regex = new Regex(configuration.CompelledSpeech);
        }
        public bool SetToSpeakSubmissively()
        {
            return
                configuration.ForcedSpeechEnabled ||
                configuration.UtterancesEnabled ||
                configuration.ForcedPronounsEnabled ||
                configuration.StutterEnabled ||
                configuration.SentenceStartEnabled ||
                configuration.SentenceEndingEnabled;
        }
        public string SubmissivelySpeak(String input)
        {
            Log.Debug($"Speak {input} submissively - Start");

            StringBuilder output = new StringBuilder();
            Random rand = new Random();
            
            List<string> words = input.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            int ticks = (int)Math.Ceiling((double)words.Count * configuration.UtteranceMaxPortionOfSpeech);
            bool to_replace = true;
            Log.Debug($"Looping over words with {words.Count}");
            for (int i = 0; i < words.Count; i++)
            {
                string full_word = words[i];
                if (isEmoji(full_word))
                {
                    Log.Debug($"{full_word} is an emoji, skipping");
                    output.Append(full_word);
                    output.Append(' ');
                    continue;
                }
                (string prefix, string word, string postfix) = handlePunctuation(full_word);
                string processed_postfix = "";
                if (prefix != "")
                {
                    if (prefix.Contains('*'))
                        to_replace = false;
                    output.Append(prefix);
                }
                if (to_replace)
                {
                    output.Append(ProcessWord(rand, word, i, words.Count, ref ticks, ref processed_postfix));
                }
                else
                {
                    output.Append(word);
                }
                if (postfix != "")
                {
                    if (postfix.Contains('*'))
                        to_replace = true;
                    output.Append(postfix);
                }
                else if (processed_postfix != "")
                {
                    output.Append(processed_postfix);
                }
                output.Append(' ');
            }
            var final = output.ToString();
            if (configuration.SentenceStartEnabled)
            {
                final = SentenceStart(final);
            }
            if (configuration.SentenceEndingEnabled)
            {
                final = SentenceEnding(final);
            }
            return final;
        }
        
        private bool isEmoji(string full_word)
        {
            return emoji_regex.IsMatch(full_word);
        }

        /// <summary>
        /// Handles the punctuation of individual word so that things like *word, are handled properly.
        /// </summary>
        /// <param name="full_word"></param>
        /// <returns>tuple that indicates a prefix punctuation, the word content, the postfix punctuation</returns>
        private (string, string, string) handlePunctuation (string full_word) {
            var captures = word_regex.Match(full_word).Groups;
            var prefix = captures[1].ToString();
            var word = captures[2].ToString();
            var postfix = captures[3].ToString();
            return (prefix, word, postfix);
        }

        // Takes the word and processes it based on the specific words
        public string ProcessWord(Random rand, string word, int word_index, int total_words, ref int ticks, ref string postfix_punctuation)
        {
            // By default it will be empty
            postfix_punctuation = "";
            // Forced speech trumps all other speech and returns immediately, no further processing required.
            if (configuration.ForcedSpeechEnabled)
            {
                Log.Debug($"Process {word} using forced speech and early returning");
                int index = rand.Next(configuration.ForcedWords.Count);
                return configuration.ForcedWords[index];
            }
            // If this is found in a pronouns list, change it.
            if (configuration.ForcedPronounsEnabled)
            {
                var key = word.ToLower();
                if (configuration.PronounsReplacements.ContainsKey(key))
                {
                    Log.Debug($"Process {word} using forced pronouns");
                    word = configuration.PronounsReplacements[key];
                }
            }
            // Roll for stuttering. TODO: Make configurable.
                if (configuration.StutterEnabled && rand.Next(100) < configuration.StutterChance)
                {
                    Log.Debug($"Process {word} and making it stutter");
                    string stutter = "";
                int max_stutters = rand.Next(configuration.MaxStutterSeverity);
                    for (int i = 0; i < 1 + max_stutters; i++)
                {
                    stutter += word.First() + "-";
                }
                    // Randomize it with a slightly more drammatic stutter
                    rand.Next(configuration.MaxStutterSeverity);

                    word = stutter + word;
                }
            // Finally if there is an utterance required, add it.
            // The number of ticks here is to respect the maximum portion of a sentence
            // (to prevent RNG from making people have a spasm of utterances unless they want to.)
            if (configuration.UtterancesEnabled && ticks > 0)
            {
                Log.Debug($"Process {word} verbal ticks");

                // Simple bias to try to ensure that you are twice as likely to have something occur near the end as the beginning
                // 2f * 1 / 10 = much lower chance to roll sufficiently
                // 2f * 5 / 10 = 1f (no mod to roll)
                // 2f * 10 / 10 = almost double the roll bias.
                // Note: For simplicity, the roll calculation is measuring less than for the chance .
                var bias = total_words / (1 + word_index);
                var roll = rand.NextDouble() * bias;
                if (configuration.Utterances.Count == 0)
                {
                    Log.Debug($"No verbal ticks found, this should not be possible to set, so there is an error in your configuration.");
                }
                else if (roll < configuration.UtteranceChance)
                {
                    ticks -= 1;
                    int index = rand.Next(configuration.Utterances.Count);
                    string tick = configuration.Utterances[index];
                    word = word + ", " + tick;
                    postfix_punctuation = ",";

                }
            }
            return word;
        }

        public string Pronouns(string input) {
            StringBuilder output = new StringBuilder();
            // This should just be a simple key lookup replacement as the definition for this should be kept within a dictionary.
            foreach (string full_word in input.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (isEmoji(full_word))
                {
                    output.Append(full_word);
                    continue;
                }
                (string pre, string word, string post) = handlePunctuation(full_word);

                output.Append(pre);
                if (configuration.PronounsReplacements.ContainsKey(word))
                {
                    output.Append(configuration.PronounsReplacements[word]);
                }
                else
                {
                    output.Append(word);
                }
                output.Append(post);
                output.Append(" ");
            }
            return output.ToString().TrimEnd();
        }
        
        public string SentenceStart(string input)
        {
            if (configuration.SentenceStartEnabled)
            {
                return configuration.SentenceStarts + input;
            }
            else
            {
                return input;
            }
        }
        public string SentenceEnding(string input)
        {
            if (configuration.SentenceEndingEnabled)
            {
                return input + configuration.SentenceEndings;
            }
            else
            {
                return input;
            }
        }
    }
    public class Speaker {
        const string WORD_REGEX = @"([^a-zA-Z0-9]*)([a-zA-Z0-9\-\']*)([^a-zA-Z0-9]*)";
// This emoji should support the following basic text emojis
        const string EMOJI_REGEX = @"^[:;cDxX><\-\,)CP][:;><\-3)CWwdpP]*[:;cDxX><\-\,3)CWwdpP]$";
        
        
        Configuration configuration;
        Regex wordRegex;
        Regex emojiRegex;
        Regex complianceRegex;
        public Speaker(Configuration configuration) {
            this.configuration = configuration;

            wordRegex = new Regex(WORD_REGEX);
            emojiRegex = new Regex(EMOJI_REGEX);
            complianceRegex = new Regex(this.configuration.CompelledSpeech);
        }

        private bool isEmoji(string full_word) {
            return emojiRegex.IsMatch(full_word);
        }

        /// <summary>
        /// Handles the punctuation of individual word so that things like *word, are handled properly.
        /// </summary>
        /// <param name="full_word"></param>
        /// <returns>tuple that indicates a prefix punctuation, the word content, the postfix punctuation</returns>
        private (string, string, string) handlePunctuation (string full_word) {
            var captures = wordRegex.Match(full_word).Groups;
            var prefix = captures[1].ToString();
            var word = captures[2].ToString();
            var postfix = captures[3].ToString();
            return (prefix, word, postfix);
        }
        // Forced speech will replace all words with one of the words in the "forced_speech" list
        // All phrases are case insensitive
        public string ForcedSpeech(string input) {
            StringBuilder output = new StringBuilder();
            Random rand = new Random();
            bool to_force = true;
            foreach(string full_word in input.Split(" ", StringSplitOptions.RemoveEmptyEntries)) {
                if (isEmoji(full_word)) {
                    output.Append(full_word);
                    output.Append(' ');
                    continue;
                }
                (string prefix, string word, string postfix) = handlePunctuation(full_word);
                if (prefix != "") {
                    if (prefix.Contains('*'))
                        to_force = false;
                    output.Append(prefix);
                }
                if (to_force) {
                    int index = rand.Next(configuration.ForcedWords.Count);
                    output.Append(configuration.ForcedWords[index]);
                } else {
                    output.Append(word);
                }
                if (postfix != "") {
                    if (postfix.Contains('*'))
                        to_force = true;
                    output.Append(postfix);
                }
                output.Append(' ');
            }
            return output.ToString();
        }

        // Locked speech will delete any words that do not match the "allowed words/phrases" list
        // All phrases are case insensitive
        public string LockedSpeech(string input) {
            StringBuilder output = new StringBuilder();
            bool lockspeech = true;
            if (!complianceRegex.ToString().Equals(configuration.CompelledSpeech)) {
                complianceRegex = new Regex(configuration.CompelledSpeech);
            }
            foreach (string full_word in input.Split(" ", StringSplitOptions.RemoveEmptyEntries)) {
                if (isEmoji(full_word)) {
                    output.Append(full_word);
                    output.Append(' ');
                    continue;
                }
                (string prefix, string word, string postfix) = handlePunctuation(full_word);
                if (prefix.Contains('*'))
                    lockspeech = false;

                if (lockspeech && configuration.AllowedWords.Contains(word.ToLower())) {
                    if (prefix != "") {
                        if (prefix.Contains('*'))
                            lockspeech = false;
                        output.Append(prefix);
                    }
                    output.Append(word);
                    if (postfix != "") {
                        output.Append(postfix);
                    }
                    output.Append(' ');
                }

                if (prefix.Contains('*'))
                    lockspeech = true;
            }
            return output.ToString();
        }

        /// <summary>
        /// Utterances are phrases randomly distributed through a sentence.
        /// 
        /// Note: The maximum number of utterances is controlled by Configuruation.UtteranceChance, but to prevent excessive utterances 
        /// </summary>
        /// <param name="input">the input string from chat</param>
        /// <returns>The chat string to be sent to the server</returns>
        public string Utterances(string input) {
            Random rand = new Random();
            StringBuilder output = new StringBuilder();
            List<string> words = input.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            bool to_utter = true;
            int utterances = (int)Math.Ceiling((double)words.Count * configuration.UtteranceMaxPortionOfSpeech);
            for (int i = 0; i<words.Count; i++) {
                if (isEmoji(words[i])) {
                    output.Append(words[i]);
                    output.Append(' ');
                    continue;
                }

                (string prefix, string word, string postfix) = handlePunctuation(words[i]);
                if (prefix!= "") {
                    if (prefix.Contains('*'))
                        to_utter = false;
                    output.Append(prefix);
                }
                // This controls the RNG aspect for this
                // Utterances is to control the quantity of the utterances
                // The roll determines the chance.
                var roll = rand.NextDouble();
                if (to_utter && utterances > 0 && roll < configuration.UtteranceChance) {
                    utterances -= 1;
                    // output.Remove(output.Length-1, 1);
                    if (i > 0) {
                        output.Remove(output.Length-1, 1);
                        output.Append(", ");
                    }
                    int index = rand.Next(configuration.Utterances.Count);
                    output.Append(configuration.Utterances[index]);
                    output.Append(", ");
                }
                output.Append(word);
                if (postfix != "") {
                    output.Append(postfix);
                }
                output.Append(' ');
            }

            return output.ToString();
        }
    }
}