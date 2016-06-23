using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitHubTracker
{
    [Export(typeof(IGitHubClient))]
    internal class GitHubHttpClient : HttpClient, IGitHubClient
    {
        private static readonly IssueStatus s_defaultStatus = IssueStatus.Open;
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        public GitHubHttpClient()
        {
            BaseAddress = new Uri("https://api.github.com");
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));
        }

        public async Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue)
        {
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Get, $"repos/{organization}/{repo}/issues/{issue}"))
                using (var result = await SendAsync(message))
                {
                    var limits = new LimitHeaders(result.Headers);

                    if (!result.IsSuccessStatusCode)
                    {
                        Debugger.Break();
                        return s_defaultStatus;
                    }

                    using (var content = await result.Content.ReadAsStreamAsync())
                    using (var textReader = new StreamReader(content))
                    using (var reader = new JsonTextReader(textReader))
                    {
                        var response = s_serializer.Deserialize<IssueResponse>(reader);

                        return response.State;
                    }
                }
            }
            catch (HttpRequestException)
            {
                Debugger.Break();
                return s_defaultStatus;
            }
        }

        private class IssueResponse
        {
            public IssueStatus State { get; set; }
        }

        private class LimitHeaders
        {
            public LimitHeaders(HttpResponseHeaders headers)
            {
                Limit = ToLong(GetHeader(headers, "X-RateLimit-Limit"));
                Remaining = ToLong(GetHeader(headers, "X-RateLimit-Remaining"));
                Reset = DateTimeOffset.FromUnixTimeSeconds(ToLong(GetHeader(headers, "X-RateLimit-Reset")));
            }

            public long Limit { get; }

            public long Remaining { get; }

            public DateTimeOffset Reset { get; }

            private static long ToLong(string value)
            {
                long i;

                if (long.TryParse(value, out i))
                {
                    return i;
                }
                else
                {
                    Debugger.Break();
                    return 0;
                }
            }

            private static string GetHeader(HttpResponseHeaders headers, string name)
            {
                IEnumerable<string> values;
                if (headers.TryGetValues(name, out values))
                {
                    Debug.Assert(values.Count() == 1);
                    return values.First();
                }
                else
                {
                    Debugger.Break();
                    return string.Empty;
                }
            }
        }
    }
}
