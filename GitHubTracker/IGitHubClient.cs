using System.Threading.Tasks;

namespace GitHubTracker
{
    public enum IssueStatus
    {
        Unavailable,
        Open,
        Closed,
        RateLimited
    };

    public interface IGitHubClient
    {
        Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue);
    }
}
