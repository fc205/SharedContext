using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;

namespace UiPathTeam.SharedContext.Activities.Dependencies
{
    internal sealed class BookmarkResumptionHelper : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy _instance;

        internal void ResumeBookmark(Bookmark bookmark, object value)
        {
            _instance.EndResumeBookmark(
                _instance.BeginResumeBookmark(bookmark, value, null, null));
        }

        internal void BeginResumeBookmark(Bookmark bookmark, object value)
        {
            _instance.BeginResumeBookmark(bookmark, value, null, null);
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            yield break;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            _instance = instance;
        }
    }
}
