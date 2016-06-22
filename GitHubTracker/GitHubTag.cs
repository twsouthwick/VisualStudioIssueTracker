using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitHubTracker
{
    internal class GitHubTag : IGlyphTag
    {
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        private readonly GitHubHttpClient _client;
        private Task<string> _task;

        public GitHubTag(string organization, string repo, int issue, GitHubHttpClient client)
        {
            _client = client;

            Organization = organization;
            Repo = repo;
            Issue = issue;

            _task = Task.Run(() =>
            {
                return GetStatusAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debugger.Break();

                        return string.Empty;
                    }

                    return t.Result;
                });
            });
        }

        public string Organization { get; }

        public string Repo { get; }

        public int Issue { get; }

        public void Update(Action<string> action)
        {
            Debug.Assert(action != null);

            _task = _task.ContinueWith(t =>
            {
                action(t.Result);
                return t.Result;
            });
        }

        private async Task<string> GetStatusAsync()
        {
            using (var message = new HttpRequestMessage(HttpMethod.Get, $"repos/{Organization}/{Repo}/issues/{Issue}"))
            using (var result = await _client.SendAsync(message))
            {
                if (!result.IsSuccessStatusCode)
                {
                    Debugger.Break();
                    return string.Empty;
                }

                using (var content = await result.Content.ReadAsStreamAsync())
                using (var textReader = new StreamReader(content))
                using (var reader = new JsonTextReader(textReader))
                {
                    var issue = s_serializer.Deserialize<IssueResponse>(reader);

                    return issue.State;
                }
            }
        }

        private class IssueResponse
        {
            public string State { get; set; }
        }
    }
}
