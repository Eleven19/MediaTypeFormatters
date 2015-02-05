using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using CsvHelper;
using CsvHelper.Configuration;
using Eleven19.Net.Http.Formatting.Internal;

namespace Eleven19.Net.Http.Formatting
{
    public class CsvMediaTypeFormatter : BufferedMediaTypeFormatter
    {
        private readonly ICsvMediaTypeFormatterConfiguration _configuration;

        public CsvMediaTypeFormatter():this(new CsvMediaTypeFormatterConfiguration())
        {
        }

        public CsvMediaTypeFormatter(ICsvMediaTypeFormatterConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/csv"));
            _configuration = configuration;
        }

        public ICsvMediaTypeFormatterConfiguration Configuration
        {
            get { return _configuration; }
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            return type.IsClosedTypeOf(typeof(IEnumerable<>));
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            // Give priority to the filename query string parameter
            var queryStrings = request.RequestUri.ParseQueryString();
            var filename = GetFilename(request, queryStrings);
            var delimiterOverride = GetDelimiter(request, queryStrings);
            return new PerInstanceCsvMediaTypeFormatter
            {
                Filename = filename,
                DelimiterOverride = delimiterOverride
            };
        }

        protected virtual string GetFilename(HttpRequestMessage request, NameValueCollection queryStrings)
        {
            var filename = queryStrings["filename"];
            if (filename == null)
            {
                object value;
                // find the filename from the route
                if (request.GetRouteData().Values.TryGetValue("filename", out value))
                {
                    if (value != null)
                    {
                        filename = value.ToString();
                    }
                }
                else
                {
                    // find the filename from te properties
                    if (request.Properties.TryGetValue("filename", out value))
                    {
                        if (value != null)
                        {
                            filename = value.ToString();
                        }
                    }
                }                
            }
            return filename;
        }

        protected virtual string GetDelimiter(HttpRequestMessage request, NameValueCollection queryStrings)
        {
            var delimiter = queryStrings["delimiter"];
            if (delimiter == null)
            {
                object value;
                if (request.Properties.TryGetValue("delimiter", out value))
                {
                    if (value != null)
                    {
                        delimiter = value.ToString();
                    }
                }
            }
            return delimiter;
        }

        internal class PerInstanceCsvMediaTypeFormatter : CsvMediaTypeFormatter
        {
            public string Filename { get; set; }
            public string DelimiterOverride { get; set; }
            public CsvConfiguration CsvConfiguration { get; set; }

            public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
            {
                base.SetDefaultContentHeaders(type, headers, mediaType);
                if (!string.IsNullOrEmpty(Filename))
                {
                    headers.Add("Content-Disposition", string.Format("attachment; filename={0}", Filename));
                }
            }
            public override void WriteToStream(Type type, object value, Stream writeStream, HttpContent content)
            {
                using (var writer = new StreamWriter(writeStream))
                {
                    var config = CsvConfiguration ?? new CsvConfiguration();
                    if (!String.IsNullOrEmpty(DelimiterOverride))
                    {
                        config.Delimiter = DelimiterOverride;
                    }

                    var csv = new CsvWriter(writer, config);
                    var records = value as IEnumerable;
                    csv.WriteRecords(records);
                }
            } 
        }        
    }
}
