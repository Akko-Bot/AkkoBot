using AkkoCore.Extensions;
using AkkoTests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AkkoTests.Core.Extensions
{
    public sealed class LinqExtTest
    {
        private readonly List<MockObject> _nullCollection = null;
        private readonly List<MockObject> _dummiesEmpty = Enumerable.Empty<MockObject>().ToList();
        private readonly List<MockObject> _dummies;
        private readonly List<MockObject> _dummiesSubcollectionTrue;
        private readonly List<MockObject> _dummiesSubcollectionFalse;
        private readonly List<MockObject> _dummiesWithNull;

        public LinqExtTest()
        {
            _dummies = Enumerable.Range(0, 10)
                .Select(x => new MockObject(x, char.ConvertFromUtf32(65 + x)))
                .ToList();

            _dummiesSubcollectionTrue = Enumerable.Range(0, 10)
                .Where(x => x % 2 is 0)
                .Select(x => new MockObject(x, char.ConvertFromUtf32(65 + x)))
                .ToList();

            _dummiesSubcollectionFalse = Enumerable.Range(0, 12)
                .Where(x => x % 2 is 0)
                .Select(x => new MockObject(x, char.ConvertFromUtf32(65 + x)))
                .ToList();

            _dummiesWithNull = _dummies.ToList();
            _dummiesWithNull[3] = null;
            _dummiesWithNull[7] = null;
        }

        [Fact]
        internal void ToConcurrentDictionaryTest()
        {
            var sample = _dummies.ToConcurrentDictionary(x => x.Id);

            Assert.Equal(10, sample.Count);
            Assert.All(sample, x => x.Key.Equals(x.Value));         // Verify if string key == string value
            Assert.All(sample, x => _dummies.Contains(x.Value));    // Verify if elements exist in the original collection
            Assert.All(_dummies, x => sample.Values.Contains(x));   // Verify is all original elements are in the sample

            // Empty collection
            sample = Enumerable.Empty<MockObject>().ToConcurrentDictionary(x => x.Id);

            Assert.NotNull(sample);
            Assert.Empty(sample);

            // Null input
            Assert.Throws<ArgumentNullException>(() => _dummies.ToConcurrentDictionary<int, MockObject>(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ToConcurrentDictionary<int, MockObject>(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ToConcurrentDictionary(x => x.Id));
        }

        [Fact]
        internal void ToConcurrentHashSetTest()
        {
            var sample = _dummies.ToConcurrentHashSet();

            Assert.Equal(10, sample.Count);
            Assert.All(sample, x => _dummies.Contains(x));  // Verify if elements exist in the original collection
            Assert.All(_dummies, x => sample.Contains(x));  // Verify is all original elements are in the sample

            // Empty collection
            sample = Enumerable.Empty<MockObject>().ToConcurrentHashSet();

            Assert.NotNull(sample);
            Assert.Empty(sample);

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ToConcurrentHashSet());
        }

        [Fact]
        internal void ContainsSubcollectionTest()
        {
            Assert.True(_dummies.ContainsSubcollection(_dummiesSubcollectionTrue));
            Assert.False(_dummies.ContainsSubcollection(_dummiesSubcollectionFalse));
            Assert.False(_dummies.ContainsSubcollection(Enumerable.Empty<MockObject>()));

            // Null input
            Assert.Throws<ArgumentNullException>(() => _dummies.ContainsSubcollection(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ContainsSubcollection(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ContainsSubcollection(_dummiesSubcollectionTrue));
        }

        [Fact]
        internal void ContainsOneTest()
        {
            Assert.True(_dummies.ContainsOne(new MockObject(0, char.ConvertFromUtf32(65))));
            Assert.True(_dummies.ContainsOne(_dummiesSubcollectionTrue));
            Assert.True(_dummies.ContainsOne(_dummiesSubcollectionFalse));
            Assert.False(_dummies.ContainsOne());
            Assert.False(_dummies.ContainsOne(Enumerable.Empty<MockObject>()));

            // Null input
            Assert.Throws<ArgumentNullException>(() => _dummies.ContainsOne(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ContainsOne(null));
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ContainsOne(_dummies));
        }

        [Fact]
        internal void DistinctByTest()
        {
            var sample = _dummies
                .Concat(_dummiesSubcollectionTrue)
                .DistinctBy(x => x.Id)
                .ToList();

            Assert.Equal(10, sample.Count);
            Assert.Contains(sample, x => _dummies.Contains(x));
            Assert.Empty(Enumerable.Empty<MockObject>().DistinctBy(x => x.Id).ToArray());

            // Null elements
            Assert.NotEmpty(_dummiesWithNull.DistinctBy(x => x?.Id).ToArray());

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.DistinctBy(x => x.Id).ToArray());
            Assert.Throws<ArgumentNullException>(() => _dummies.DistinctBy<MockObject, int>(null).ToArray());
            Assert.Throws<NullReferenceException>(() => _dummiesWithNull.DistinctBy(x => x.Id).ToArray());
        }

        [Fact]
        public async Task ToListAsync()
        {
            var sample = await Enumerable.Range(0, 10)
                .Select(x => Task.Run(() => new MockObject(x, char.ConvertFromUtf32(65 + x))))
                .ToListAsync();

            Assert.All(sample, x => _dummies.Contains(x));

            // Null elements
            Assert.NotEmpty(await _dummiesWithNull.Select(x => Task.Run(() => new MockObject(x?.Id ?? default, char.ConvertFromUtf32(65 + x?.Id ?? default)))).ToListAsync());

            // Null input
            await Assert.ThrowsAsync<ArgumentNullException>(() => _nullCollection.Select(x => Task.Run(() => new MockObject(x.Id, char.ConvertFromUtf32(65 + x.Id)))).ToListAsync());
            await Assert.ThrowsAsync<NullReferenceException>(() => _dummiesWithNull.Select(x => Task.Run(() => new MockObject(x.Id, char.ConvertFromUtf32(65 + x.Id)))).ToListAsync());
        }

        [Fact]
        internal void ExceptByTest()
        {
            // Should only contain odd ids
            var sample = _dummies
                .ExceptBy(_dummiesSubcollectionTrue, x => x.Id)
                .ToArray();

            Assert.Equal(5, sample.Length);
            Assert.DoesNotContain(sample, x => x.Id % 2 is 0);

            // Should only contain odd ids
            sample = _dummiesSubcollectionTrue
                .ExceptBy(_dummies, x => x.Id)
                .ToArray();

            Assert.Equal(5, sample.Length);
            Assert.DoesNotContain(sample, x => x.Id % 2 is 0);

            // Empty collections
            Assert.NotEmpty(_dummiesEmpty.ExceptBy(_dummies, x => x.Id).ToArray());
            Assert.NotEmpty(_dummies.ExceptBy(_dummiesEmpty, x => x.Id).ToArray());

            // Null elements
            Assert.NotEmpty(_dummies.ExceptBy(_dummiesWithNull, x => x?.Id).ToArray());

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.ExceptBy(_dummies, x => x.Id).ToArray());
            Assert.Throws<ArgumentNullException>(() => _dummies.ExceptBy(_nullCollection, x => x.Id).ToArray());
            Assert.Throws<ArgumentNullException>(() => _dummies.ExceptBy<MockObject, int>(_dummiesSubcollectionTrue, null).ToArray());
            Assert.Throws<NullReferenceException>(() => _dummies.ExceptBy(_dummiesWithNull, x => x.Id).ToArray());
        }

        [Fact]
        internal void IntersectByTest()
        {
            // Should only contain even ids
            var sample = _dummies
                .IntersectBy(_dummiesSubcollectionFalse, x => x.Id)
                .ToArray();

            Assert.Equal(5, sample.Length);
            Assert.DoesNotContain(sample, x => x.Id % 2 is not 0);

            // Should only contain even ids
            sample = _dummiesSubcollectionFalse
                .IntersectBy(_dummies, x => x.Id)
                .ToArray();

            Assert.Equal(5, sample.Length);
            Assert.DoesNotContain(sample, x => x.Id % 2 is not 0);

            // Empty collections
            Assert.Empty(_dummiesEmpty.IntersectBy(_dummies, x => x.Id).ToArray());
            Assert.Empty(_dummies.IntersectBy(_dummiesEmpty, x => x.Id).ToArray());

            // Null elements
            Assert.NotEmpty(_dummies.IntersectBy(_dummiesWithNull, x => x?.Id).ToArray());

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.IntersectBy(_dummies, x => x.Id).ToArray());
            Assert.Throws<ArgumentNullException>(() => _dummies.IntersectBy(_nullCollection, x => x.Id).ToArray());
            Assert.Throws<ArgumentNullException>(() => _dummies.IntersectBy<MockObject, int>(_dummiesSubcollectionTrue, null).ToArray());
            Assert.Throws<NullReferenceException>(() => _dummies.IntersectBy(_dummiesWithNull, x => x.Id).ToArray());
        }

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

        [Fact]
        internal void SplitIntoTest()
        {
            var sample = _dummiesWithNull.SplitInto(5);

            Assert.Equal(2, sample.Count);

            foreach (var collection in sample)
                Assert.Equal(5, collection.Count);

            // 0 input
            sample = _dummies.SplitInto(0);

            Assert.Equal(_dummies.Count, sample.Count);

            // Big input
            sample = _dummies.SplitInto(_dummies.Count * 2);

            Assert.NotEmpty(sample);
            Assert.Equal(_dummies.Count, sample[0].Count);

            // Empty collection
            sample = _dummiesEmpty.SplitInto(5);

            Assert.NotEmpty(sample);
            Assert.Empty(sample[0]);

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.SplitInto(5));
        }

        [Fact]
        internal void SplitByTest()
        {
            var counter = 1;
            var sample = _dummies
                .Select(x => new MockObject((x.Id < 5) ? 1 : 2, x.Name))
                .SplitBy(x => x.Id);

            Assert.Equal(2, sample.Count);

            foreach (var collection in sample)
            {
                Assert.Equal(5, collection.Count);
                Assert.All(collection, x => x.Id.Equals(counter));
                counter++;
            }

            // Empty collection
            Assert.Empty(_dummiesEmpty.SplitBy(x => x.Id));

            // Null elements
            Assert.Throws<ArgumentNullException>(() => _dummiesWithNull.SplitBy(x => x?.Id));

            // Null input
            Assert.Throws<ArgumentNullException>(() => _nullCollection.SplitBy(x => x.Id));
            Assert.Throws<ArgumentNullException>(() => _dummies.SplitBy<MockObject, int>(null));
            Assert.Throws<NullReferenceException>(() => _dummiesWithNull.SplitBy(x => x.Id));
        }
    }
}