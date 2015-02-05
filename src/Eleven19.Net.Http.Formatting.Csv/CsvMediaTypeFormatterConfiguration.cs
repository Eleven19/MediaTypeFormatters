using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CsvHelper.Configuration;

namespace Eleven19.Net.Http.Formatting
{
    internal class CsvMediaTypeFormatterConfiguration : ICsvMediaTypeFormatterConfiguration
    {
        private readonly IDictionary<Type, Func<CsvConfiguration>> _configurationMappings = 
            new ConcurrentDictionary<Type, Func<CsvConfiguration>>();
        public ICsvMediaTypeFormatterConfiguration UseConfiguration(Type type, Func<CsvConfiguration> configurationFactory)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (configurationFactory == null) throw new ArgumentNullException("configurationFactory");
            _configurationMappings[type] = configurationFactory;
            return this;
        }

        public bool TryGetConfigurationFor(Type type, out CsvConfiguration configuration)
        {
            if (type == null) throw new ArgumentNullException("type");
            Func<CsvConfiguration> factory;
            if (_configurationMappings.TryGetValue(type, out factory))
            {
                configuration = factory.Invoke();
                return true;
            }
            configuration = null;
            return false;
        }
    }
}