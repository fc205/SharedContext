using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;

namespace UiPath.Core.Activities
{
    public class MonitorEvents : NativeActivity
    {
        protected override bool CanInduceIdle
        {
            get
            {
                return true; // we create bookmarks
            }
        }

        [Category("Common")]
        [DisplayName("Repeat Forever")]
        public Activity<bool> RepeatForever { get; set; }

        [Category("Common")]
        [DisplayName("Continue On Error")]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        public ActivityAction<object> Handler { get; set; }

        [Browsable(false)]
        public List<Activity> Triggers { get; private set; }

        protected Queue<KeyValuePair<ActivityAction<object>, object>> EventQueue = new Queue<KeyValuePair<ActivityAction<object>, object>>();

        private Bookmark _monitorBookmark;

        private BookmarkResumptionHelper _bookmarkResumptionHelper;

        private Variable<NoPersistHandle> _noPersistHandle;

        public MonitorEvents()
        {
            Triggers = new List<Activity>();
            Handler = new ActivityAction<object>
            {
                Handler = new Sequence { DisplayName = "Event Handler" },
                Argument = new DelegateInArgument<object>("args")
            };
            RepeatForever = true;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            _noPersistHandle = new Variable<NoPersistHandle>();
            metadata.AddImplementationVariable(_noPersistHandle);
            RepeatForever = RepeatForever ?? false;
            metadata.AddChild(RepeatForever);
            foreach (var trigger in Triggers)
            {
                metadata.AddChild(trigger);
            }
            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider<BookmarkResumptionHelper>(() => new BookmarkResumptionHelper());
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            try
            {
                EventQueue.Clear();
                _monitorBookmark = context.CreateBookmark(OnMonitorTrigger, BookmarkOptions.MultipleResume);
                _bookmarkResumptionHelper = context.GetExtension<BookmarkResumptionHelper>();
                context.Properties.Add("MonitorBookmark", _monitorBookmark, true);
                StartMonitor(context);
            }
            catch (Exception ex)
            {
                DisposeMonitor(context);
                HandleException(ex, ContinueOnError.Get(context));
            }
        }

        protected void StartMonitor(NativeActivityContext context)
        {
            _noPersistHandle.Get(context).Enter(context);
            foreach (var trigger in Triggers)
            {
                context.ScheduleActivity(trigger);
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            DisposeMonitor(context);
            base.Cancel(context);
        }

        private void OnMonitorTrigger(NativeActivityContext context, System.Activities.Bookmark bookmark, object value)
        {
            EventQueue.Enqueue(new KeyValuePair<ActivityAction<object>, object>(Handler, value));
            if (EventQueue.Count == 1)
            {
                ExecuteEventHandler(context);
            }
        }

        protected void ExecuteEventHandler(NativeActivityContext context)
        {
            var eventInfo = EventQueue.Peek();
            if (eventInfo.Key != null)
            {
                // schedule handler passing the info to it
                context.ScheduleAction(eventInfo.Key, eventInfo.Value, BodyCompleted, BodyFaulted);
            }
        }

        protected virtual void BodyCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            // evaluate RepeatForever to see if next scheduling should be done
            context.ScheduleActivity(RepeatForever, RepeatForeverCompleted);
        }

        protected void RepeatForeverCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result)
        {
            if (!result)
            {
                DisposeMonitor(context);
            }
            else
            {
                EventQueue.Dequeue();
                if (EventQueue.Count > 0)
                {
                    ExecuteEventHandler(context);
                }
            }
        }

        protected void BodyFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            DisposeMonitor(faultContext);
            HandleException(propagatedException, ContinueOnError.Get(faultContext));
            faultContext.CancelChildren();
            faultContext.HandleFault();
        }

        protected void DisposeMonitor(NativeActivityContext context)
        {
            context.RemoveBookmark(_monitorBookmark);
            context.CancelChildren();
            _noPersistHandle.Get(context).Exit(context);
        }

        protected void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }
    }
}
