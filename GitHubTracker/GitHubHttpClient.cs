using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GitHubTracker
{
    [Export]
    internal class GitHubHttpClient : HttpClient
    {
        public GitHubHttpClient()
        {
            BaseAddress = new Uri("https://api.github.com");
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("GitHub_VS_Tracker", "1.0")));
        }
    }
}
