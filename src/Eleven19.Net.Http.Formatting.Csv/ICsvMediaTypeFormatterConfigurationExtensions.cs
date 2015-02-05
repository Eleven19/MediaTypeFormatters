// ReSharper disable InconsistentNaming

using System;
using CsvHelper.Configuration;

namespace Eleven19.Net.Http.Formatting
{
    public static class ICsvMediaTypeFormatterConfigurationExtensions
    {
        public static CsvConfiguration GetConfigurationFor(this ICsvMediaTypeFormatterConfiguration config, Type type)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (type == null) throw new ArgumentNullException("type");
            CsvConfiguration result;
            if (!config.TryGetConfigurationFor(type, out result))
            {
                return new CsvConfiguration();
            }
            return result;
        }
    }
}