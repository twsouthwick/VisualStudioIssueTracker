using System.Threading.Tasks;

namespace GitHubTracker
{
    public interface IGitHubClient
    {
        Task<string> GetStatusAsync(string organization, string repo, int issue);
    }
}
