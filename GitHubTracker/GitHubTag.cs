using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitHubTracker
{

    internal class GitHubTag : IGlyphTag
    {
        // TODO: Inject in to control lifetime
        private static readonly HttpClient s_client = new HttpClient();

        private Task<string> _task;

        public GitHubTag(string organization, string repo, int issue)
        {
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
            using (var message = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Organization}/{Repo}/issues/{Issue}"))
            {
                message.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));

                var result = await s_client.SendAsync(message);

                if (!result.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var serializer = JsonSerializer.CreateDefault();

                using (var content = await result.Content.ReadAsStreamAsync())
                using (var textReader = new StreamReader(content))
                using (var reader = new JsonTextReader(textReader))
                {
                    var issue = serializer.Deserialize<IssueResponse>(reader);

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
