using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace GitHubTracker
{
    [Export(typeof(IGitHubClient))]
    internal class GitHubHttpClient : HttpClient, IGitHubClient
    {
        private static readonly DataContractJsonSerializer s_serializer = new DataContractJsonSerializer(typeof(IssueResponse));

        // TODO: Set up a time-dependent cache to eject info after a set period
        private ConcurrentDictionary<IssueInfo, Task<IssueStatus>> _issues = new ConcurrentDictionary<IssueInfo, Task<IssueStatus>>(new IssueInfoComparer());

        public GitHubHttpClient()
        {
            BaseAddress = new Uri("https://api.github.com");
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));
        }

        public Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue)
        {
            var info = new IssueInfo
            {
                Organization = organization,
                Repo = repo,
                Issue = issue
            };

            // TODO: Handle retry for rate-limited value
            return _issues.GetOrAdd(info, Task.Run(async () => await GetStatusAsync(info)));
        }

        private async Task<IssueStatus> GetStatusAsync(IssueInfo info)
        {
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Get, $"repos/{info.Organization}/{info.Repo}/issues/{info.Issue}"))
                using (var result = await SendAsync(message))
                {
                    var limits = new LimitHeaders(result.Headers);

                    if (!result.IsSuccessStatusCode)
                    {
                        return limits.Remaining == 0 ? IssueStatus.RateLimited : IssueStatus.Unavailable;
                    }

                    using (var content = await result.Content.ReadAsStreamAsync())
                    {
                        var response = (IssueResponse)s_serializer.ReadObject(content);

                        return response.GetStatus();
                    }
                }
            }
            catch (HttpRequestException)
            {
                return IssueStatus.Unavailable;
            }
        }

        [DataContract]
        private class IssueResponse
        {
            [DataMember(Name = "state")]
            public string State { get; set; }

            public IssueStatus GetStatus()
            {
                IssueStatus status;
                if (Enum.TryParse(State, true, out status))
                {
                    return status;
                }

                return default(IssueStatus);
            }
        }

        private struct LimitHeaders
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

        private struct IssueInfo
        {
            public string Organization;
            public string Repo;
            public int Issue;
        }

        private class IssueInfoComparer : IEqualityComparer<IssueInfo>
        {
            public bool Equals(IssueInfo x, IssueInfo y)
            {
                return x.Issue == y.Issue
                    && string.Equals(x.Organization, y.Organization, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Repo, y.Repo, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(IssueInfo obj)
            {
                unchecked
                {
                    const int Multiplier = 23;
                    int hash = 17;

                    hash = hash * Multiplier + obj.Issue.GetHashCode();
                    hash = hash * Multiplier + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Organization);
                    hash = hash * Multiplier + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Repo);

                    return hash;
                }
            }
        }
    }
}
