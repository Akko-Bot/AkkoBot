using AkkoBot.Extensions;
using AkkoTests.Entities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions.ObjectExt
{
    public class EqualsAnyTests
    {
        private readonly List<MockObject> _dummies;

        public EqualsAnyTests()
        {
            _dummies = Enumerable.Range(0, 10)
                .Select(x => new MockObject(x, char.ConvertFromUtf32(65 + x)))
                .ToList();
        }

        [Theory]
        [InlineData(1, "B", true)]
        [InlineData(2, "C", true)]
        [InlineData(9, "J", true)]
        [InlineData(-1, "H", false)]
        [InlineData(999, "D", false)]
        [InlineData(4, "C", false)]
        [InlineData(2, null, false)]
        public void EqualsAnyTest(int id, string name, bool result)
            => Assert.Equal(result, new MockObject(id, name).EqualsAny(_dummies));

        [Theory]
        [InlineData(null, false)]
        public void EqualsAnyNullTests(object thisObject, bool result)
        {
            Assert.Equal(result, thisObject.EqualsAny(_dummies));
            Assert.Equal(result, _dummies.EqualsAny(thisObject));
            Assert.True(thisObject.EqualsAny(null));
        }
    }
}