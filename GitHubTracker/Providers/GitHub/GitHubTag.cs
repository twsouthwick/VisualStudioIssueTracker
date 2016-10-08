using Microsoft.VisualStudio.Text.Editor;
using System.Threading.Tasks;

namespace IssueTracker.Providers.GitHub
{
    internal class GitHubTag : VersionControlTag, IVersionControlTag, IGlyphTag
    {
        private readonly IGitHubClient _client;

        public GitHubTag(string organization, string repo, int issue, IGitHubClient client)
        {
            _client = client;

            Organization = organization;
            Repo = repo;
            Issue = issue;
        }

        protected override Task<IssueStatus> GetStatusAsync()
        {
            return _client.GetStatusAsync(Organization, Repo, Issue);
        }

        public string Organization { get; }

        public string Repo { get; }

        public int Issue { get; }
    }
}
