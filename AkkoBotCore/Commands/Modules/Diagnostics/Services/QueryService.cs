using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using AkkoBot.Services.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Diagnostics.Services
{
    /// <summary>
    /// Groups utility methods for querying the database manually.
    /// </summary>
    public class QueryService : ICommandService
    {
        // TODO: I may need to, in the future, specify which database needs to be queried
        private readonly IServiceProvider _services;

        public QueryService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Runs any non-SELECT query on the database.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The amount of rows affected and time, in milliseconds, the query took to run.</returns>
        /// <exception cref="PostgresException">Occurs when the query is invalid or when it tries to fetch data that does not exist.</exception>
        public async Task<(int, long)> RunExecQueryAsync(string query)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var clock = new Stopwatch();

            clock.Start();
            var rows = await db.Database.ExecuteSqlRawAsync(query);
            clock.Stop();

            return (rows, clock.ElapsedMilliseconds);
        }

        /// <summary>
        /// Runs a SELECT query on the database.
        /// </summary>
        /// <param name="query">The SQL query to be run.</param>
        /// <returns>A collection of columns from the resulting query.</returns>
        /// <exception cref="PostgresException">Occurs when the query is invalid or when it tries to fetch data that does not exist.</exception>
        public async Task<List<SerializableEmbedField>> RunSelectQueryAsync(string query)
        {
            var result = new List<SerializableEmbedField>();
            var fieldBuilders = new List<StringBuilder>();

            // Open database connection for manual read
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = await command.ExecuteReaderAsync();

            // Get the column names
            for (var column = 0; column < reader.FieldCount; column++)
            {
                // Constant so the column name doesn't get localized
                result.Add(new(AkkoConstants.ValidWhitespace + reader.GetName(column), string.Empty, true));
                fieldBuilders.Add(new());
            }

            // Get the row values
            while (await reader.ReadAsync())
            {
                var row = 0;
                var objs = new object[reader.FieldCount];
                reader.GetValues(objs);

                while (row < reader.FieldCount)
                    fieldBuilders[row].AppendLine(objs[row++].ToString());

                if (fieldBuilders.Any(x => x.Length >= AkkoConstants.EmbedFieldMaxLength))
                    break;
            }

            // Add row values to result fields
            for (var counter = 0; counter < result.Count; counter++)
            {
                result[counter].Text = (string.IsNullOrWhiteSpace(fieldBuilders[counter].ToString()))
                    ? AkkoConstants.ValidWhitespace
                    : fieldBuilders[counter].ToString();
            }

            return result;
        }
    }
}