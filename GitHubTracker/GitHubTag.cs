using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitHubTracker
{
    internal class GitHubTag : IGlyphTag
    {
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        private readonly IGitHubClient _client;
        private Task<string> _task;

        public GitHubTag(string organization, string repo, int issue, IGitHubClient client)
        {
            _client = client;

            Organization = organization;
            Repo = repo;
            Issue = issue;

            _task = Task.Run(() =>
            {
                return _client.GetStatusAsync(organization, repo, issue).ContinueWith(t =>
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
    }
}
