using Newtonsoft.Json;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitHubTracker
{
    [Export(typeof(IGitHubClient))]
    internal class GitHubHttpClient : HttpClient, IGitHubClient
    {
        private static readonly IssueStatus s_defaultStatus = IssueStatus.Open;
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        public GitHubHttpClient()
        {
            BaseAddress = new Uri("https://api.github.com");
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));
        }

        public async Task<IssueStatus> GetStatusAsync(string organization, string repo, int issue)
        {
            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Get, $"repos/{organization}/{repo}/issues/{issue}"))
                using (var result = await SendAsync(message))
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        Debugger.Break();
                        return s_defaultStatus;
                    }

                    using (var content = await result.Content.ReadAsStreamAsync())
                    using (var textReader = new StreamReader(content))
                    using (var reader = new JsonTextReader(textReader))
                    {
                        var response = s_serializer.Deserialize<IssueResponse>(reader);

                        return response.State;
                    }
                }
            }
            catch (HttpRequestException)
            {
                Debugger.Break();
                return s_defaultStatus;
            }
        }

        private class IssueResponse
        {
            public IssueStatus State { get; set; }
        }
    }
}
