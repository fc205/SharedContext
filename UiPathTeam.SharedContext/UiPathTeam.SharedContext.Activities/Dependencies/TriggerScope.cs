using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UiPath.Shared.Activities.Localization;
using UiPathTeam.SharedContext.Activities.Properties;

namespace UiPathTeam.SharedContext.Activities.Dependencies
{
    public abstract class TriggerScope<T> : NativeActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Debug_DisplayName))]
        [LocalizedDescription(nameof(Resources.Debug_Description))]
        [LocalizedCategory(nameof(Resources.Misc_Category))]
        public bool Debug { get; set; }

        /// <summary>
        /// We require an activity here so the expression can be re-evaluated
        /// multiple time after the "Do" block is complete.
        /// </summary>
        [Category("Options")]
        [DisplayName("ContinueMonitoring")]
        public Activity<bool> ContinueMonitoring { get; set; }

        [Category("Common")]
        [DisplayName("ContinueOnError")]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        public ActivityAction<T> Body { get; set; }

        private Bookmark _triggerBookmark;
        private BookmarkResumptionHelper _bookmarkResumptionHelper;

        protected Queue<T> _eventQueue = new Queue<T>();

        protected TriggerScope() : base()
        {
            Body = new ActivityAction<T>
            {
                Argument = new DelegateInArgument<T>(typeof(T).Name),
                Handler = new Sequence { DisplayName = "Do" }
            };
            this.Debug = false;
        }

        /// <summary>
        /// Implemented in the derived class to start the monitoring process.
        /// </summary>
        protected abstract void StartMonitoring(NativeActivityContext context);

        /// <summary>
        /// Implemented in the derived class to stop the monitoring process.
        /// </summary>
        protected abstract void StopMonitoring(ActivityContext context);

        /// <summary>
        /// We use bookmarks.
        /// </summary>
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider(() => new BookmarkResumptionHelper());
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            try
            {
                _eventQueue.Clear();
                _triggerBookmark = context.CreateBookmark(OnMonitorTrigger, BookmarkOptions.MultipleResume);
                _bookmarkResumptionHelper = context.GetExtension<BookmarkResumptionHelper>();
                StartMonitoring(context);
            }
            catch (Exception)
            {
                CleanUp(context);
                throw;
            }
        }

        protected void HandleEvent(object sender, T args)
        {
            if(this.Debug)
            {
                Console.WriteLine("[SharedContext Trigger] Fired!!! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            }
            if (_triggerBookmark != null)
            {
                _bookmarkResumptionHelper.BeginResumeBookmark(_triggerBookmark, args);
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            CleanUp(context);
            base.Cancel(context);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            try
            {
                StopMonitoring(context);
            }
            finally
            {
                // Clean-up even if there is an exception in the derived class.
                base.Abort(context);
            }
        }

        private void CleanUp(NativeActivityContext context)
        {
            try
            {
                StopMonitoring(context);
            }
            finally
            {
                // Clean-up even if there is an exception in the derived class.
                context.RemoveBookmark(_triggerBookmark);
                context.CancelChildren();
            }
        }

        private void OnMonitorTrigger(NativeActivityContext context, Bookmark bookmark, object value)
        {
            if (value is T valueT)
            {
                _eventQueue.Enqueue(valueT);
                if (_eventQueue.Count == 1)
                {
                    ExecuteEventHandler(context);
                }
            }
        }

        private void ExecuteEventHandler(NativeActivityContext context)
        {
            // Schedule handler passing the scope variable.
            context.ScheduleAction(Body, _eventQueue.Peek(), BodyCompleted, BodyFaulted);
        }

        private void BodyCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (ContinueMonitoring == null)
            {
                CleanUp(context);
            }
            else
            {
                // Evaluate whether we need to continue running the trigger.
                context.ScheduleActivity(ContinueMonitoring, ContinueMonitoringCompleted);
            }
        }

        private void ContinueMonitoringCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (!result)
            {
                CleanUp(context);
            }
            else
            {
                _eventQueue.Dequeue();
                if (_eventQueue.Count > 0)
                {
                    ExecuteEventHandler(context);
                }
            }
        }

        private void BodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            if (ContinueOnError.Get(faultContext))
            {
                // Cancel all remaining actions for the current event.
                faultContext.CancelChildren();

                // ... but continue monitoring and triggering new events.
                faultContext.HandleFault();

                Trace.TraceError(propagatedException.ToString());
            }
            else
            {
                CleanUp(faultContext);
                throw propagatedException;
            }
        }
    }
}
