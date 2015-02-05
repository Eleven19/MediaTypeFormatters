using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using Eleven19.Net.Http.Formatting.Csv.TestObjects;
using FakeItEasy;
using FluentAssertions;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Eleven19.Net.Http.Formatting.Csv
{
    public class CsvMediaTypeFormatterConfigurationTests
    {
        public class CallingUseConfiguration
        {
            internal CsvMediaTypeFormatterConfiguration SUT { get; private set; }

            public CallingUseConfiguration()
            {
                SUT = new CsvMediaTypeFormatterConfiguration();
            }

            [Fact]
            public void UseConfigurationShouldThrowOnNullType()
            {
                Type type = null;
                SUT.Invoking(sut => 
                    sut.UseConfiguration(type, () => new CsvConfiguration())
                ).ShouldThrow<ArgumentNullException>().Where(ex=>ex.ParamName == "type");
            }

            [Fact]
            public void UseConfigurationShouldThrowOnNullFactory()
            {
                Func<CsvConfiguration> factory = null;
                SUT.Invoking(sut =>
                    sut.UseConfiguration(typeof(Employee), factory)
                ).ShouldThrow<ArgumentNullException>().Where(ex => ex.ParamName == "configurationFactory");
            }
        }

        public class GetConfigurationFor
        {
            internal CsvMediaTypeFormatterConfiguration SUT { get; private set; }

            public GetConfigurationFor()
            {
                SUT = new CsvMediaTypeFormatterConfiguration();
            }

            [Fact]
            public void TryGetConfigurationForShouldThrowOnNullType()
            {
                Type type = null;
                SUT.Invoking(sut =>
                {
                    CsvConfiguration result;
                    sut.TryGetConfigurationFor(type, out result);
                }).ShouldThrow<ArgumentNullException>().Where(ex => ex.ParamName == "type");
            }

            [Fact]
            public void TryGetConfigurationForShouldNotThrowWhenRetrievingUnregisteredType()
            {
                SUT.Invoking(sut =>
                {
                    CsvConfiguration result;
                    sut.TryGetConfigurationFor(typeof(Employee), out result);
                }).ShouldNotThrow();
            }

            [Fact]
            public void GetConfigurationForShouldThrowOnNullType()
            {
                Type type = null;
                SUT.Invoking(sut =>
                    sut.GetConfigurationFor(type)
                ).ShouldThrow<ArgumentNullException>().Where(ex => ex.ParamName == "type");
            }

            [Fact]
            public void GetConfigurationForShouldNotThrowWhenTypeNotRegistered()
            {
                Type type = typeof(Employee);
                SUT.Invoking(sut =>
                    sut.GetConfigurationFor(type)
                ).ShouldNotThrow();
            }

            [Fact]
            public void GetConfigurationForShouldCallTryGetConfigurationFor()
            {
                var type = typeof (Employee);
                var formatterConfig = A.Fake<ICsvMediaTypeFormatterConfiguration>();
                formatterConfig.GetConfigurationFor(type);
                CsvConfiguration result;
                A.CallTo(() => formatterConfig.TryGetConfigurationFor(type, out result))
                    .MustHaveHappened(Repeated.Exactly.Once);
            }

            [Fact]
            public void GetConfigurationForShouldReturnConfigurationFromUseConfigurationCall()
            {
                Type type = typeof(Employee);
                var expectedConfig = new CsvConfiguration();
                SUT.UseConfiguration(type, () => expectedConfig);
                var actualConfig = SUT.GetConfigurationFor(type);
                actualConfig.Should().BeSameAs(expectedConfig);
            }
        }
    }
}
