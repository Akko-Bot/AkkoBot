using AkkoCore.Extensions;
using AkkoTests.Models;
using AkkoTests.TestData;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions;

public sealed partial class LinqExtTests
{
    [Theory]
    [MemberData(nameof(MockCollectionTestData.NullCollection), MemberType = typeof(MockCollectionTestData))]
    internal void ToConcurrentHashSetNullTest(IEnumerable<MockObject> collection)
        => Assert.Throws<ArgumentNullException>(() => collection.ToConcurrentHashSet());

    [Theory]
    [MemberData(nameof(MockCollectionTestData.EmptyCollection), MemberType = typeof(MockCollectionTestData))]
    internal void ToConcurrentHashSetEmptyTest(IEnumerable<MockObject> collection)
    {
        var result = collection.ToConcurrentHashSet();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [MemberData(nameof(MockCollectionTestData.Collection), MemberType = typeof(MockCollectionTestData))]
    [MemberData(nameof(MockCollectionTestData.CollectionWithNull), MemberType = typeof(MockCollectionTestData))]
    internal void ToConcurrentHashSetTest(IEnumerable<MockObject> collection)
    {
        var sample = collection.ToConcurrentHashSet();

        Assert.True(sample.All(x => collection.Contains(x)));    // Verify if elements exist in the original collection
        Assert.True(collection.All(x => sample.Contains(x)));    // Verify if all original elements are in the sample
    }
}