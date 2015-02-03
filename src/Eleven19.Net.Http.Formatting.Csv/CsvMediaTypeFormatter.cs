using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using Eleven19.Net.Http.Formatting.Internal;

namespace Eleven19.Net.Http.Formatting
{
    public class CsvMediaTypeFormatter : BufferedMediaTypeFormatter
    {
        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            return type.IsClosedTypeOf(typeof(IEnumerable<>));
        }
    }
}
