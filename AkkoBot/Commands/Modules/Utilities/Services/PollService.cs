using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoDatabase;
using AkkoDatabase.Entities;
using AkkoDatabase.Enums;
using AkkoEntities.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="PollEntity"/> objects.
    /// </summary>
    public class PollService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public PollService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds a poll to the database.
        /// </summary>
        /// <param name="message">The Discord message associated with the poll.</param>
        /// <param name="question">The question in the poll.</param>
        /// <param name="type">The type of the poll.</param>
        /// <param name="answers">Answers to the poll, empty if it's a <see cref="PollType.Simple"/> poll.</param>
        /// <returns><see langword="true"/> if the poll was successfully created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddPollAsync(DiscordMessage message, string question, PollType type, params string[] answers)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var newEntry = new PollEntity()
            {
                GuildIdFK = message.Channel.Guild.Id,
                ChannelId = message.Channel.Id,
                MessageId = message.Id,
                Type = type,
                Question = question,
                Answers = answers,
                Votes = new int[answers.Length]
            };

            // Add to the database
            db.Add(newEntry);

            var success = await db.SaveChangesAsync() is not 0;

            // Add to the cache
            if (!_dbCache.Polls.TryAdd(newEntry.GuildIdFK, new() { newEntry }))
                _dbCache.Polls[newEntry.GuildIdFK].Add(newEntry);

            return success;
        }

        /// <summary>
        /// Removes a poll from the database.
        /// </summary>
        /// <param name="message">The Discord message associated with the poll.</param>
        /// <returns><see langword="true"/> if the poll was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemovePollAsync(DiscordMessage message)
        {
            _dbCache.Polls.TryGetValue(message.Channel.Guild.Id, out var polls);
            var poll = polls.FirstOrDefault(x => x.GuildIdFK == message.Channel.Guild.Id && x.ChannelId == message.Channel.Id && x.MessageId == message.Id);

            if (poll is null)
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            db.Remove(poll);
            polls.TryRemove(poll);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes a poll from the database.
        /// </summary>
        /// <param name="poll">The database poll.</param>
        /// <returns><see langword="true"/> if the poll was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemovePollAsync(PollEntity poll)
        {
            if (poll is null)
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            _dbCache.Polls.TryGetValue(poll.GuildIdFK, out var polls);

            db.Remove(poll);
            polls.TryRemove(poll);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Registers a vote for an anonymous poll.
        /// </summary>
        /// <param name="user">The Discord user that cast their vote.</param>
        /// <param name="poll">The database poll.</param>
        /// <param name="vote">The vote.</param>
        /// <returns><see langword="true"/> if the vote was successfully cast, <see langword="false"/> otherwise.</returns>
        public async Task<bool> VoteAsync(DiscordUser user, PollEntity poll, int vote)
        {
            if (poll is null || poll.Type is not PollType.Anonymous || vote > poll.Votes.Length || poll.Voters.Contains((long)user.Id))
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            poll.Votes[vote - 1]++;
            poll.Voters.Add((long)user.Id);

            db.Update(poll);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Get all polls from the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>A collection of polls.</returns>
        public IReadOnlyCollection<PollEntity> GetPolls(DiscordGuild server)
        {
            _dbCache.Polls.TryGetValue(server.Id, out var polls);
            return polls ?? new(1, 0);
        }

        /// <summary>
        /// Removes a poll from the database, edits the Discord message associated with the poll,
        /// removes all reactions from it and sends a copy of the edited message to the current context.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="message">The Discord message associated with the poll.</param>
        /// <param name="poll">The database poll.</param>
        /// <returns>A copy of the closed poll message.</returns>
        public async Task<DiscordMessage> FinishUpPollAsync(CommandContext context, DiscordMessage message, PollEntity poll)
        {
            var result = GetPollResult(context, message, poll);

            if (result.Embed is not null)
            {
                var tempEmbed = new DiscordEmbedBuilder(result.Embed);

                tempEmbed.Title += $" - {context.FormatLocalized("poll_closed")}";
                result.Embed = tempEmbed;
            }

            var closedPoll = await message.ModifyAsync(result);
            await RemovePollAsync(message);
            await closedPoll.DeleteAllReactionsAsync();

            return await context.RespondAsync(closedPoll.Content, (result.Embed is null) ? null : closedPoll.Embeds[0]);
        }

        /// <summary>
        /// Gets the result of the specified poll.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="message">The poll message.</param>
        /// <param name="poll">The database poll.</param>
        /// <returns>The poll results.</returns>
        public DiscordMessageBuilder GetPollResult(CommandContext context, DiscordMessage message, PollEntity poll)
        {
            return poll.Type switch
            {
                PollType.Simple => GenerateSimplePollResult(new DiscordMessageBuilder(), message, GetPollReactions(message, AkkoStatics.ThumbsUpEmoji, AkkoStatics.ThumbsDownEmoji).ToArray()),
                PollType.Numeric => GenerateNumericPollResult(context, new DiscordMessageBuilder(), message, poll, GetPollReactions(message, AkkoStatics.NumericEmojis.Take(poll.Answers.Length))),
                PollType.Anonymous => GenerateAnonymousPollResult(context, new DiscordMessageBuilder(), message, poll),
                _ => throw new NotImplementedException($@"Poll of type ""{poll.Type}"" has not been implemented."),
            };
        }

        /// <summary>
        /// Gets the current votes of the specified poll.
        /// </summary>
        /// <param name="message">The Discord message associated with the poll.</param>
        /// <param name="votes">The emojis being used for voting.</param>
        /// <returns>The collection of reactions used for voting.</returns>
        private IEnumerable<DiscordReaction> GetPollReactions(DiscordMessage message, params DiscordEmoji[] votes)
            => message.Reactions.Where(x => x.Emoji.EqualsAny(votes));

        /// <summary>
        /// Gets the current votes of the specified poll.
        /// </summary>
        /// <param name="message">The Discord message associated with the poll.</param>
        /// <param name="votes">The emojis being used for voting.</param>
        /// <returns>The collection of reactions used for voting.</returns>
        private IEnumerable<DiscordReaction> GetPollReactions(DiscordMessage message, IEnumerable<DiscordEmoji> votes)
            => message.Reactions.Where(x => x.Emoji.EqualsAny(votes));

        /// <summary>
        /// Gets the results of a simple poll.
        /// </summary>
        /// <param name="msgBuilder">The new message.</param>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="result">The votes cast in the poll.</param>
        /// <returns>The poll results.</returns>
        /// <exception cref="ArgumentException">Occurs when there are not exactly 2 results.</exception>
        private DiscordMessageBuilder GenerateSimplePollResult(DiscordMessageBuilder msgBuilder, DiscordMessage oldMessage, DiscordReaction[] result)
        {
            if (result.Length != 2)
                throw new ArgumentException("Simple polls must have exactly 2 options.", nameof(result));

            msgBuilder.Content = oldMessage.Content;

            if (oldMessage.Embeds.Count >= 1)
            {
                // Message with embed
                var newEmbed = new DiscordEmbedBuilder(oldMessage.Embeds[0])
                    .WithUrl(oldMessage.JumpLink.AbsoluteUri);

                newEmbed.Description +=
                    $"\n\n{result[0].Emoji} - {result[0].Count - 1} | " +
                    $"{result[1].Emoji} - {result[1].Count - 1}";

                msgBuilder.Embed = newEmbed;
            }
            else
            {
                // Message without embed
                msgBuilder.Content +=
                    $"\n\n{result[0].Emoji} - {result[0].Count - 1} | " +
                    $"{result[1].Emoji} - {result[1].Count - 1}";
            }

            return msgBuilder;
        }

        /// <summary>
        /// Gets the results of a numeric poll.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="msgBuilder">The new message.</param>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="poll">The database poll.</param>
        /// <param name="result">The votes cast in the poll.</param>
        /// <returns>The poll results.</returns>
        /// <exception cref="ArgumentException">Occurs when less than 2 results are provided.</exception>
        private DiscordMessageBuilder GenerateNumericPollResult(CommandContext context, DiscordMessageBuilder msgBuilder, DiscordMessage oldMessage, PollEntity poll, IEnumerable<DiscordReaction> result)
        {
            if (result.Count() < 2)
                throw new ArgumentException("Numeric polls must have at least 2 options.", nameof(result));

            msgBuilder.Content = oldMessage.Content;
            var descriptionBuilder = new StringBuilder(
                (oldMessage.Embeds.Count >= 1)
                    ? string.Empty
                    : oldMessage.Content[..(oldMessage.Content.IndexOf(poll.Question) + poll.Question.Length + 2)] + "\n\n"
            );

            foreach (var reaction in result.OrderByDescending(x => x.Count))
            {
                var answerIndex = GetNumericEmojiValue(reaction.Emoji) - 1;
                descriptionBuilder.AppendLine($"{poll.Answers[answerIndex]} {context.FormatLocalized("with_votes", reaction.Count - 1)}");
            }

            if (oldMessage.Embeds.Count >= 1)
            {
                // Message with embed
                var newEmbed = new DiscordEmbedBuilder(oldMessage.Embeds[0])
                    .WithUrl(oldMessage.JumpLink.AbsoluteUri);

                newEmbed.Description = descriptionBuilder.ToString();
                msgBuilder.Embed = newEmbed;
            }
            else
            {
                // Message without embed
                descriptionBuilder.AppendLine(oldMessage.JumpLink.AbsoluteUri);
                msgBuilder.Content = descriptionBuilder.ToString();
            }

            return msgBuilder;
        }

        /// <summary>
        /// Gets the results of an anonymous poll.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="msgBuilder">The new message.</param>
        /// <param name="oldMessage">The old message.</param>
        /// <param name="poll">The database poll.</param>
        /// <returns>The poll results.</returns>
        private DiscordMessageBuilder GenerateAnonymousPollResult(CommandContext context, DiscordMessageBuilder msgBuilder, DiscordMessage oldMessage, PollEntity poll)
        {
            msgBuilder.Content = oldMessage.Content;
            var counter = 0;
            var answers = poll.Answers.ToDictionary(x => x, y => poll.Votes[counter++]);
            var descriptionBuilder = new StringBuilder(
                (oldMessage.Embeds.Count >= 1)
                    ? string.Empty
                    : oldMessage.Content[..(oldMessage.Content.IndexOf(poll.Question) + poll.Question.Length + 2)] + "\n\n"
            );

            foreach (var answer in answers.OrderByDescending(x => x.Value))
                descriptionBuilder.AppendLine($"{answer.Key} {context.FormatLocalized("with_votes", answer.Value)}");

            if (oldMessage.Embeds.Count >= 1)
            {
                // Message with embed
                var newEmbed = new DiscordEmbedBuilder(oldMessage.Embeds[0])
                    .WithUrl(oldMessage.JumpLink.AbsoluteUri);

                newEmbed.Description = descriptionBuilder.ToString();
                msgBuilder.Embed = newEmbed;
            }
            else
            {
                // Message without embed
                descriptionBuilder.AppendLine(oldMessage.JumpLink.AbsoluteUri);
                msgBuilder.Content = descriptionBuilder.ToString();
            }

            answers.Clear();

            return msgBuilder;
        }

        /// <summary>
        /// Converts a numeric emoji to its integer value.
        /// </summary>
        /// <param name="emoji">A Discord emoji.</param>
        /// <returns>The value represented by the emoji.</returns>
        /// <exception cref="ArgumentException">Occurs when the emoji is not numeric.</exception>
        private int GetNumericEmojiValue(DiscordEmoji emoji)
        {
            return emoji.GetDiscordName() switch
            {
                ":zero:" => 0,
                ":one:" => 1,
                ":two:" => 2,
                ":three:" => 3,
                ":four:" => 4,
                ":five:" => 5,
                ":six:" => 6,
                ":seven:" => 7,
                ":eight:" => 8,
                ":nine:" => 9,
                ":keycap_ten:" => 10,
                _ => throw new ArgumentException("The specified emoji is not numeric.", nameof(emoji))
            };
        }
    }
}