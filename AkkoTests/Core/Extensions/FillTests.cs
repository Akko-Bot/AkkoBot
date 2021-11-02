using AkkoCore.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions
{
    public sealed partial class LinqExtTests
    {
        //public static IEnumerable<object[]> StringCollection1 { get; } = new string[][] { new string[] { "Some strings", "This should create 5 slots" } };
        //public static IEnumerable<object[]> StringCollection2 { get; } = new string[][] { new string[] { "This should create 5 slots", "Some strings" } };
        //public static IEnumerable<object[]> StringCollection3 { get; } = new string[][] { new string[] { "This should not", "Change at all" } };

        [Fact]
        internal void FillTest()
        {
            var sample = new string[] { "Some strings", "This should create 5 slots" }
                .Select(x => x.Split(' '))
                .Fill(string.Empty);

            Assert.Equal(2, sample.Count);

            foreach (var collection in sample)
                Assert.Equal(5, collection.Count);

            // Empty collection
            sample = Array.Empty<string>()
                .Select(x => x.Split(' '))
                .Fill(string.Empty);

            Assert.Empty(sample);

            // Null elements
            Assert.Throws<ArgumentNullException>(() => new string[] { null, "This should create 5 slots" }.Select(x => x?.Split(' ')).Fill(string.Empty));

            // Null input
            string[] nullStrings = null;
            Assert.Throws<ArgumentNullException>(() => nullStrings.Select(x => x?.Split(' ')).Fill(string.Empty));
            Assert.Throws<ArgumentNullException>(() => Array.Empty<string>().Select(x => x.Split(' ')).Fill<string, IEnumerable<string>>(null));
        }
    }
}