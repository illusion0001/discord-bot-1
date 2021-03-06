﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CompatApiClient.Utils;
using CompatBot.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace CompatBot.EventHandlers
{
    public static class UsernameZalgoMonitor
    {
        private static readonly HashSet<char> OversizedChars = new HashSet<char>
        {
            '꧁', '꧂', '⎝', '⎠', '⧹', '⧸', '⎛', '⎞',
        };

        public static async Task OnUserUpdated(UserUpdateEventArgs args)
        {
            try
            {
                var m = args.Client.GetMember(args.UserAfter);
                if (NeedsRename(m.DisplayName))
                {
                    var suggestedName = StripZalgo(m.DisplayName, m.Id).Sanitize();
                    await args.Client.ReportAsync("🔣 Potential display name issue",
                        $"User {m.GetMentionWithNickname()} has changed their __username__ and is now shown as **{m.DisplayName.Sanitize()}**\nSuggestion to rename: **{suggestedName}**",
                        null,
                        ReportSeverity.Medium);
                    await DmAndRenameUserAsync(args.Client, args.Client.GetMember(args.UserAfter), suggestedName).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Config.Log.Error(e);
            }
        }

        public static async Task OnMemberUpdated(GuildMemberUpdateEventArgs args)
        {
            try
            {
                var name = args.Member.DisplayName;
                if (NeedsRename(name))
                {
                    var suggestedName = StripZalgo(name, args.Member.Id).Sanitize();
                    await args.Client.ReportAsync("🔣 Potential display name issue",
                        $"Member {args.Member.GetMentionWithNickname()} has changed their __display name__ and is now shown as **{name.Sanitize()}**\nSuggestion to rename: **{suggestedName}**",
                        null,
                        ReportSeverity.Medium);
                    await DmAndRenameUserAsync(args.Client, args.Member, suggestedName).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Config.Log.Error(e);
            }
        }

        public static async Task OnMemberAdded(GuildMemberAddEventArgs args)
        {
            try
            {
                var name = args.Member.DisplayName;
                if (NeedsRename(name))
                {
                    var suggestedName = StripZalgo(name, args.Member.Id).Sanitize();
                    await args.Client.ReportAsync("🔣 Potential display name issue",
                        $"New member joined the server: {args.Member.GetMentionWithNickname()} and is shown as **{name.Sanitize()}**\nSuggestion to rename: **{suggestedName}**",
                        null,
                        ReportSeverity.Medium);
                    await DmAndRenameUserAsync(args.Client, args.Member, suggestedName).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Config.Log.Error(e);
            }
        }

        public static bool NeedsRename(string displayName)
        {
            displayName = displayName?.Normalize().TrimEager();
            return displayName != StripZalgo(displayName, 0ul, NormalizationForm.FormC, 3);
        }

        private static async Task DmAndRenameUserAsync(DiscordClient client, DiscordMember member, string suggestedName)
        {
            try
            {
                var renameTask = member.ModifyAsync(m => m.Nickname = suggestedName);
                Config.Log.Info($"Renamed {member.Username}#{member.Discriminator} ({member.Id}) to {suggestedName}");
                var rulesChannel = await client.GetChannelAsync(Config.BotRulesChannelId).ConfigureAwait(false);
                var msg = $"Hello, your current _display name_ is breaking {rulesChannel.Mention} #7, so you have been renamed to `{suggestedName}.\n" +
                          "I'm not perfect and can't clean all the junk in names in some cases, so change your nickname at your discretion.\n" +
                          "You can change your _display name_ by clicking on the server name at the top left and selecting **Change Nickname**.";
                var dm = await member.CreateDmChannelAsync().ConfigureAwait(false);
                await dm.SendMessageAsync(msg).ConfigureAwait(false);
                await renameTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Config.Log.Warn(e);
            }
        }

        public static string StripZalgo(string displayName, ulong userId, NormalizationForm normalizationForm = NormalizationForm.FormD, int level = 0)
        {
            displayName = displayName?.Normalize(normalizationForm).TrimEager();
            if (string.IsNullOrEmpty(displayName))
                return "Rule #7 Breaker #" + userId.GetHashCode().ToString("x8");

            var builder = new StringBuilder();
            bool skipLowSurrogate = false;
            int consecutive = 0;
            foreach (var c in displayName)
            {
                switch (char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.ModifierSymbol:
                    case UnicodeCategory.NonSpacingMark:
                        if (++consecutive < level)
                            builder.Append(c);
                        break;

                    case UnicodeCategory.Control:
                    case UnicodeCategory.Format:
                        break;

                    case UnicodeCategory.OtherNotAssigned when c >= 0xdb40:
                        skipLowSurrogate = true;
                        break;

                    default:
                        if (char.IsLowSurrogate(c) && skipLowSurrogate)
                            skipLowSurrogate = false;
                        else
                        {
                            if (!OversizedChars.Contains(c))
                                builder.Append(c);
                            consecutive = 0;
                        }
                        break;
                }
            }
            var result = builder.ToString().TrimEager();
            if (string.IsNullOrEmpty(result))
                return "Rule #7 Breaker #" + userId.GetHashCode().ToString("x8");

            return result;
        }
    }
}
