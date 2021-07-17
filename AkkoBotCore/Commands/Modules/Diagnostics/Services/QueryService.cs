using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable.EmbedParts;
using AkkoBot.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Collections;
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
        private readonly IServiceScopeFactory _scopeFactory;

        public QueryService(IServiceScopeFactory scopeFactory)
            => _scopeFactory = scopeFactory;

        /// <summary>
        /// Runs any non-SELECT query on the database.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The amount of rows affected and time, in milliseconds, the query took to run.</returns>
        /// <exception cref="PostgresException">Occurs when the query is invalid or when it tries to fetch data that does not exist.</exception>
        public async Task<(int, double)> RunExecQueryAsync(string query)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var clock = new Stopwatch();
            clock.Start();
            var rows = await db.Database.ExecuteSqlRawAsync(query);
            clock.Stop();

            return (rows, clock.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Runs a SELECT query on the database.
        /// </summary>
        /// <param name="query">The SQL query to be run.</param>
        /// <returns>A collection of columns from the resulting query and time, in milliseconds, that it took to run.</returns>
        /// <exception cref="PostgresException">Occurs when the query is invalid or when it tries to fetch data that does not exist.</exception>
        public async Task<(IReadOnlyCollection<SerializableEmbedField>, double)> RunSelectQueryAsync(string query)
        {
            var result = new List<SerializableEmbedField>();
            var fieldBuilders = new List<StringBuilder>();

            // Open database connection for manual read
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            var clock = new Stopwatch();
            clock.Start();  // Start counting query execution time.
            using var reader = await command.ExecuteReaderAsync();

            // Get the column names
            for (var column = 0; column < reader.FieldCount; column++)
            {
                result.Add(new(reader.GetName(column), string.Empty, true));
                fieldBuilders.Add(new());
            }

            // Get the row values
            while (await reader.ReadAsync())
            {
                var row = 0;
                var objs = new object[reader.FieldCount];
                reader.GetValues(objs);

                if (objs.All(x => x is null))
                    continue;

                while (row < reader.FieldCount)
                {
                    if (objs[row] is not ICollection collection)
                        fieldBuilders[row].AppendLine(objs[row].ToString());
                    else
                    {
                        fieldBuilders[row].Append('{');

                        foreach (var obj in collection)
                            fieldBuilders[row].Append(obj.ToString() + ", ");

                        if (collection.Count is not 0)
                            fieldBuilders[row].Remove(fieldBuilders[row].Length - 2, 2);

                        fieldBuilders[row].AppendLine("}");
                    }

                    row++;
                }
            }

            clock.Stop();   // Stop counting query execution time.

            // Add row values to result fields
            for (var counter = 0; counter < result.Count; counter++)
            {
                result[counter].Text = (string.IsNullOrWhiteSpace(fieldBuilders[counter].ToString()))
                    ? string.Empty
                    : fieldBuilders[counter].ToString();
            }

            return (result, clock.Elapsed.TotalMilliseconds);
        }
    }
}