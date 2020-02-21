using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Get Information about the current process")]
    public class GetProcessInfoActivity : CodeActivity
    {
        [Category("Output")]
        [DisplayName("Job Id")]
        [Description("Job Id")]
        public OutArgument<string> JobId { get; set; }

        [Category("Output")]
        [DisplayName("Process Name")]
        [Description("Process Name")]
        public OutArgument<string> ProcessName { get; set; }

        [Category("Output")]
        [DisplayName("Process Version")]
        [Description("Process Version")]
        public OutArgument<string> ProcessVersion { get; set; }

        [Category("Output")]
        [DisplayName("Workflow File Path")]
        [Description("Workflow File Path")]
        public OutArgument<string> WorkflowFilePath { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var executorRuntime = context.GetExtension<IExecutorRuntime>();
            var jobInfo = executorRuntime.RunningJobInformation;

            JobId.Set(context, jobInfo.JobId.ToString());
            ProcessName.Set(context, jobInfo.ProcessName.ToString());
            ProcessVersion.Set(context, jobInfo.ProcessVersion.ToString());
            WorkflowFilePath.Set(context, jobInfo.WorkflowFilePath.ToString());
        }
    }
}
