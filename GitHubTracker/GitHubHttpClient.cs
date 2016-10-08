using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        private static readonly DataContractJsonSerializer s_serializer = new DataContractJsonSerializer(typeof(GitHubIssueResponse));

        private readonly ConcurrentDictionary<IssueDetails, IssueStatusCacheItem> _issueCache = new ConcurrentDictionary<IssueDetails, IssueStatusCacheItem>(new IssueDetailsComparer());

        public GitHubHttpClient()
        {
            BaseAddress = new Uri("https://api.github.com");
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));
        }

        public Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue)
        {
            var info = new IssueDetails
            {
                Organization = organization,
                Repo = repo,
                Issue = issue
            };

            return GetStatusAsync(info);
        }

        private async Task<IssueStatus> GetStatusAsync(IssueDetails info)
        {
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Get, $"repos/{info.Organization}/{info.Repo}/issues/{info.Issue}"))
                {
                    IssueStatusCacheItem status;
                    if (_issueCache.TryGetValue(info, out status))
                    {
                        message.Headers.TryAddWithoutValidation("If-None-Match", status.Headers.ETag);
                    }

                    using (var response = await SendAsync(message))
                    {
                        var headers = new GithubHeaders(response.Headers);

                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            return status.Status;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            return headers.Remaining == 0 ? IssueStatus.RateLimited : IssueStatus.Unavailable;
                        }

                        using (var content = await response.Content.ReadAsStreamAsync())
                        {
                            var issueResponse = (GitHubIssueResponse)s_serializer.ReadObject(content);

                            var item = new IssueStatusCacheItem
                            {
                                Headers = headers,
                                Status = issueResponse.GetStatus()
                            };

                            _issueCache.AddOrUpdate(info, item, (_, __) => item);

                            return item.Status;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                return IssueStatus.Unavailable;
            }
        }

        [DataContract]
        private class GitHubIssueResponse
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

        [DebuggerDisplay("GitHub: {Remaining}/{Limit}")]
        private struct GithubHeaders
        {
            public GithubHeaders(HttpResponseHeaders headers)
            {
                Limit = ToLong(GetHeader(headers, "X-RateLimit-Limit"));
                Remaining = ToLong(GetHeader(headers, "X-RateLimit-Remaining"));
                Reset = DateTimeOffset.FromUnixTimeSeconds(ToLong(GetHeader(headers, "X-RateLimit-Reset")));
                ETag = GetHeader(headers, "ETag");
            }

            public long Limit { get; }

            public long Remaining { get; }

            public DateTimeOffset Reset { get; }

            public string ETag { get; }

            private static long ToLong(string value)
            {
                long i;

                if (long.TryParse(value, out i))
                {
                    return i;
                }
                else
                {
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
                    return string.Empty;
                }
            }
        }

        [DebuggerDisplay("{Organization}/{Repo}/{Issue}")]
        private struct IssueDetails
        {
            public string Organization;
            public string Repo;
            public int Issue;
        }

        [DebuggerDisplay("{Status}")]
        private struct IssueStatusCacheItem
        {
            public IssueStatus Status;
            public GithubHeaders Headers;
        }

        private class IssueDetailsComparer : IEqualityComparer<IssueDetails>
        {
            public bool Equals(IssueDetails x, IssueDetails y)
            {
                return x.Issue == y.Issue
                    && string.Equals(x.Organization, y.Organization, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Repo, y.Repo, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(IssueDetails obj)
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
