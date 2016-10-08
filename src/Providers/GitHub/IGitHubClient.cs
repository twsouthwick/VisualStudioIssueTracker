using System.Threading.Tasks;

namespace IssueTracker.Providers.GitHub
{
    public interface IGitHubClient
    {
        Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue);
    }
}
