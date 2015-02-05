using System;
using CsvHelper.Configuration;

namespace Eleven19.Net.Http.Formatting
{
    public interface ICsvMediaTypeFormatterConfiguration
    {
        ICsvMediaTypeFormatterConfiguration UseConfiguration(Type type, Func<CsvConfiguration> configurationFactory);
        bool TryGetConfigurationFor(Type type, out CsvConfiguration configuration);
    }
}