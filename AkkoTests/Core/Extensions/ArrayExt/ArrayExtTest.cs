using AkkoEntities.Extensions;
using System;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions.ArrayExt
{
    public class ArrayExtTest
    {
        [Theory]
        [InlineData(50)]
        [InlineData(10)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void TryGetValueTestTrue(int arraySize)
        {
            var normalizedArraySize = Math.Abs(arraySize);
            var sample = Enumerable
                .Range(0, normalizedArraySize)
                .ToArray();

            for (var index = 0; index < normalizedArraySize; index++)
            {
                // Test all elements in the array
                Assert.True(sample.TryGetValue(index, out var element));
                Assert.Equal(index, element);
            }

            // Test last element in the array
            if (normalizedArraySize is not 0)
            {
                Assert.True(sample.TryGetValue(normalizedArraySize - 1, out var lastElement));
                Assert.Equal(normalizedArraySize - 1, lastElement);
            }

            // Test one index after last element
            Assert.False(sample.TryGetValue(normalizedArraySize, out var defaultInt));
            Assert.Equal(default, defaultInt);

            // Test index out of bounds (lower)
            Assert.False(sample.TryGetValue(-1, out defaultInt));
            Assert.Equal(default, defaultInt);
        }
    }
}