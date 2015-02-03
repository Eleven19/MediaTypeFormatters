using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleven19.Net.Http.Formatting.Internal;
using FluentAssertions;
using Xunit.Extensions;

namespace Eleven19.Net.Http.Formatting.Csv.Internal
{
    public class TypeExtensionsTests
    {
        public class IsClosedTypeOfTest
        {
            public static IEnumerable<object[]> Examples
            {
                get
                {
                    yield return new object[] {typeof (List<string>), typeof (IEnumerable<>), true};
                    yield return new object[] { typeof(List<string>), typeof(IDictionary<,>), false };
                    yield return new object[] { typeof(Dictionary<string,object>), typeof(IDictionary<,>), true };
                }
            }

            [Theory]
            [PropertyData("Examples")]
            public void IsClosedTypeOfShouldReturnExpectedResults(Type targetType, Type testType, bool expectedResult)
            {
                var actualResult = targetType.IsClosedTypeOf(testType);
                actualResult.Should().Be(expectedResult);
            }
        }
    }
}
