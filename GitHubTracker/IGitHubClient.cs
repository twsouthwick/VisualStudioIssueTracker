using System.Threading.Tasks;

namespace GitHubTracker
{
    public enum IssueStatus { Open, Closed, All };

    public interface IGitHubClient
    {
        Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue);
    }
}
