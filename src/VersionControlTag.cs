using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IssueTracker
{
    internal abstract class VersionControlTag : IVersionControlTag
    {
        private Task<IssueStatus> _task;

        public void Update(Action<IssueStatus> action)
        {
            Debug.Assert(action != null);

            if (_task == null)
            {
                Initialize();
            }

            _task = _task.ContinueWith(t =>
            {
                action(t.Result);
                return t.Result;
            });
        }

        public void Initialize()
        {
            _task = Task.Run(async () => await GetStatusAsync());
        }

        protected abstract Task<IssueStatus> GetStatusAsync();
    }
}
