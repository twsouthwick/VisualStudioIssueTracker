using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitHubTracker
{
    internal class GitHubTag : IGlyphTag
    {
        private readonly IGitHubClient _client;
        private Task<IssueStatus> _task;

        public GitHubTag(string organization, string repo, int issue, IGitHubClient client)
        {
            _client = client;

            Organization = organization;
            Repo = repo;
            Issue = issue;

            _task = Task.Run(() =>
            {
                return _client.GetStatusAsync(organization, repo, issue);
            });
        }

        public string Organization { get; }

        public string Repo { get; }

        public int Issue { get; }

        public void Update(Action<IssueStatus> action)
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
