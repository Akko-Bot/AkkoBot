﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [Group("poll")]
    [Description("cmd_poll")]
    public class Poll : AkkoCommandModule
    {
        private readonly IDbCache _dbCache;
        private readonly PollService _service;

        public Poll(IDbCache dbCache, PollService service)
        {
            _dbCache = dbCache;
            _service = service;
        }

        [Command("startsimple"), Aliases("simple")]
        [Description("cmd_poll_startsimple")]
        [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
        public async Task CreateSimplePoll(CommandContext context, [RemainingText, Description("arg_poll_question")] string question)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle(context.FormatLocalized("poll_title", context.User.GetFullname()))
                .WithDescription(Formatter.Bold(question));

            var pollMsg = await context.RespondLocalizedAsync(embed, false);

            if (await _service.AddPollAsync(pollMsg, question, PollType.Simple))
                await pollMsg.CreateReactionsAsync(AkkoEntities.ThumbsUpEmoji, AkkoEntities.ThumbsDownEmoji);
            else
            {
                await pollMsg.DeleteAsync();
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
            }
        }

        [Command("startnumeric"), Aliases("numeric")]
        [Description("cmd_poll_startnumeric")]
        [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
        public async Task CreateNumericPoll(CommandContext context, [RemainingText, Description("arg_poll_qanswer")] string questionAndAnswers)
        {
            var qAnswers = questionAndAnswers.Split(';');
            var pollMsg = await CreateNumeratedPollAsync(context, PollType.Numeric, qAnswers);

            if (pollMsg is not null)
                await pollMsg.CreateReactionsAsync(AkkoEntities.NumericEmojis.Take(qAnswers.Length - 1));
        }

        [Command("startanonymous"), Aliases("anonymous")]
        [Description("cmd_poll_startanonymous")]
        [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
        public async Task CreateAnonymousPoll(CommandContext context, [RemainingText, Description("arg_poll_qanswer")] string questionAndAnswers)
        {
            if (_service.GetPolls(context.Guild).Any(x => x.Type is PollType.Anonymous))
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("poll_anonymous_error");

                await context.RespondLocalizedAsync(embed, isError: true);
                return;
            }

            await CreateNumeratedPollAsync(context, PollType.Anonymous, questionAndAnswers.Split(';'));
        }

        [GroupCommand, Command("status"), Aliases("check", "result")]
        [Description("cmd_poll_status")]
        public async Task ShowPollStatus(CommandContext context, [Description("arg_discord_message_link")] DiscordMessage message = null)
        {
            var polls = _service.GetPolls(context.Guild);

            var poll = (message is null)
                ? polls.FirstOrDefault(x => x.ChannelId == context.Channel.Id)
                : polls.FirstOrDefault(x => x.MessageId == message.Id);

            if (poll is null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("poll_not_found");

                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                message ??= await poll.GetPollMessageAsync(context.Guild);

                if (message is not null)
                {
                    var result = _service.GetPollResult(context, message, poll);
                    await context.Channel.SendMessageAsync(result);
                }
                else
                {
                    await _service.RemovePollAsync(poll);
                    await context.Message.CreateReactionAsync(AkkoEntities.WarningEmoji);
                }
            }
        }

        [Command("stop"), Aliases("end")]
        [Description("cmd_poll_stop")]
        [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
        public async Task RemovePoll(CommandContext context, [Description("arg_discord_message_link")] DiscordMessage message = null)
        {
            var polls = _service.GetPolls(context.Guild);

            var poll = (message is null)
                ? polls.FirstOrDefault(x => x.ChannelId == context.Channel.Id)
                : polls.FirstOrDefault(x => x.MessageId == message.Id);

            if (poll is null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("poll_not_found");

                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                message ??= await poll.GetPollMessageAsync(context.Guild);

                if (message is not null)
                    await _service.FinishUpPollAsync(context, message, poll);
                else
                {
                    await _service.RemovePollAsync(poll);
                    await context.Message.CreateReactionAsync(AkkoEntities.WarningEmoji);
                }
            }
        }

        [Command("forcestop"), Aliases("forceend")]
        [Description("cmd_poll_forcestop")]
        [RequireGuild, RequirePermissions(Permissions.ManageGuild | Permissions.ManageMessages)]
        public async Task ForceRemovePoll(CommandContext context, [Description("arg_poll_id")] int id)
        {
            var poll = _service.GetPolls(context.Guild).FirstOrDefault(x => x.Id == id);
            await context.Message.CreateReactionAsync((await _service.RemovePollAsync(poll)) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("vote")]
        [Description("cmd_poll_vote")]
        [RequireDirectMessage]
        public async Task Vote(CommandContext context, [Description("arg_poll_vote")] int option, [Description("arg_discord_message_link")] DiscordMessage message)
        {
            var poll = _service.GetPolls(message.Channel.Guild)
                .FirstOrDefault(x => x.GuildIdFK == message.Channel.Guild.Id && x.ChannelId == message.Channel.Id && x.MessageId == message.Id);

            var embed = new DiscordEmbedBuilder();
            var success = poll is not null && await _service.VoteAsync(context.User, poll, option);

            embed.Description = (success)
                ? context.FormatLocalized("voted_for", "\n" + poll.Answers[option - 1])
                : "vote_failure"; // Poll doesn't exist or user has voted already

            await context.RespondLocalizedAsync(embed, isError: !success);
        }

        [Command("list"), Aliases("show")]
        [Description("cmd_poll_list")]
        [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
        public async Task ListPolls(CommandContext context)
        {
            var polls = _service.GetPolls(context.Guild);
            var embed = new DiscordEmbedBuilder();

            if (!polls.Any())
            {
                embed.WithDescription("poll_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fields = new List<SerializableEmbedField>();
            embed.WithTitle("poll_list_title");

            foreach (var pollGroup in polls.SplitInto(AkkoConstants.LinesPerPage))
            {
                fields.Add(new("id", string.Join("\n", pollGroup.Select(x => x.Id)), true));
                fields.Add(new("poll", string.Join("\n", pollGroup.Select(x => x.Question.MaxLength(50, "[...]"))), true));
                fields.Add(new("channel", string.Join("\n", pollGroup.Select(x => $"<#{x.ChannelId}>")), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 3);
        }

        /// <summary>
        /// Creates a poll with multiple choices.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="pollType">The type of the poll.</param>
        /// <param name="qAnswers">Question in the first index, followed by the answers to the poll.</param>
        /// <returns>The poll that was just created.</returns>
        private async Task<DiscordMessage> CreateNumeratedPollAsync(CommandContext context, PollType pollType, string[] qAnswers)
        {
            var embed = new DiscordEmbedBuilder();

            if (qAnswers.Length < 3 || (pollType is PollType.Numeric && qAnswers.Length > 11))
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return null;
            }

            var counter = 0;
            var answers = qAnswers
                .Skip(1)
                .Select(x => Formatter.InlineCode($"{++counter}.") + " " + Formatter.Bold(x))
                .ToArray();

            embed.WithTitle(context.FormatLocalized("poll_title", context.User.GetFullname()))
                .WithDescription(Formatter.Bold(qAnswers[0]) + "\n\n" + string.Join("\n", answers));

            if (pollType is PollType.Anonymous)
                embed.WithFooter(context.FormatLocalized("poll_anonymous_footer", $@"""{_dbCache.BotConfig.BotPrefix}poll vote"""));

            var pollMsg = await context.RespondLocalizedAsync(embed, false);
            await _service.AddPollAsync(pollMsg, qAnswers[0], pollType, answers);

            return pollMsg;
        }
    }
}