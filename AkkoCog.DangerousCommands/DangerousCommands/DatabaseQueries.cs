using AkkoCog.DangerousCommands.Services;
using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Npgsql;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoCog.DangerousCommands.Diagnostics // Integrate with 'Diagnostics'
{
    [BotOwner]
    public sealed class DatabaseQueries : AkkoCommandModule
    {
        private readonly QueryService _service;

        public DatabaseQueries(QueryService service)
            => _service = service;

        [Command("sql")]
        [Description("cmd_sql")]
        public async Task ExecuteQueryAsync(CommandContext context, [RemainingText, Description("arg_sql")] string query)
        {
            if (query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                await SqlSelectAsync(context, query);
            else
                await SqlExecAsync(context, query);
        }

        /// <summary>
        /// Sends the result of any non-SELECT query to Discord.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="query">The SQL query.</param>
        private async Task SqlExecAsync(CommandContext context, string query)
        {
            var question = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("q_are_you_sure", "q_sql_query", "q_yes", "q_no"));

            await context.RespondInteractiveAsync(question, "q_yes", async () =>
            {
                var result = await _service.RunExecQueryAsync(query).RunAndGetTaskAsync();

                if (result.IsFaulted)
                {
                    await SendPsqlErrorAsync(context, result);
                    return;
                }

                var (rows, time) = await result;

                var embed = new SerializableDiscordEmbed()
                    .WithDescription(context.FormatLocalized("sqlexec_success", time.ToString("0.00"), rows));

                await context.RespondLocalizedAsync(embed);
            });
        }

        /// <summary>
        /// Sends the result of a SELECT query to Discord.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="query">The SQL query.</param>
        private async Task SqlSelectAsync(CommandContext context, string query)
        {
            var result = await _service.RunSelectQueryAsync(query).RunAndGetTaskAsync();

            if (result.IsFaulted)
            {
                await SendPsqlErrorAsync(context, result);
                return;
            }

            var (fields, time) = await result;

            var text = SerializableDiscordEmbed.DecomposeEmbedFields(fields)
                .Replace("\r", string.Empty);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text[..text.LastOccurrenceOf('\n', 2)]));    // Substring to remove the last row of empty values

            var message = new DiscordMessageBuilder()
                .WithContent(context.FormatLocalized("sqlselect_title", time.ToString("0.00"), fields.FirstOrDefault()?.Text.Occurrences('\n') ?? 0))
                .WithFile("query_result.txt", stream);

            await context.Channel.SendMessageAsync(message);
        }

        /// <summary>
        /// Sends a Discord message with information about the database exception from a task.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="task">An awaited <see cref="Task"/>.</param>
        /// <exception cref="AggregateException">Occurs when <see cref="Task.Exception"/> is not of base type <see cref="PostgresException"/>.</exception>
        /// <exception cref="ArgumentNullException">Occurs when <see cref="Task.Exception"/> is null.</exception>
        private async Task SendPsqlErrorAsync(CommandContext context, Task task)
        {
            if (task?.Exception is null)
                throw new ArgumentNullException(nameof(task), "Task cannot be null.");
            if (task.Exception.GetBaseException() is not PostgresException exception)
                throw task.Exception;

            var embed = new SerializableDiscordEmbed()
                .WithDescription($"{exception.Severity} ({exception.SqlState}): {exception.MessageText}\n" + exception.Hint);

            await context.RespondLocalizedAsync(embed, isError: true);
        }
    }
}