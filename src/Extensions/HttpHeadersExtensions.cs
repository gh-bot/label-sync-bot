using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Label.Synchronizer.Bot
{
    public static class HttpHeadersExtensions
    {
        public static string ValueOrDefault(this IHeaderDictionary headers, string name)
        {
            if (headers.TryGetValue(name, out var values))
                return values.FirstOrDefault();

            return null;
        }
    }
}
