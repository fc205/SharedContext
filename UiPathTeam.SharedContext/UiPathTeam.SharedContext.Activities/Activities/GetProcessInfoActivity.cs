using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetProcessInfoActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetProcessInfoActivity_Description))]
    public class GetProcessInfoActivity : CodeActivity
    {
        [LocalizedDisplayName(nameof(Resources.GetProcessInfoActivity_JobId_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetProcessInfoActivity_JobId_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> JobId { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetProcessInfoActivity_ProcessName_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetProcessInfoActivity_ProcessName_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ProcessName { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetProcessInfoActivity_ProcessVersion_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetProcessInfoActivity_ProcessVersion_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ProcessVersion { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetProcessInfoActivity_WorflowFilePath_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetProcessInfoActivity_WorflowFilePath_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> WorkflowFilePath { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var executorRuntime = context.GetExtension<IExecutorRuntime>();
            var jobInfo = executorRuntime.RunningJobInformation;

            string currentProcess = jobInfo.ProcessName.ToString();
            if (currentProcess.Contains("_"))
            {
                currentProcess = currentProcess.Split('_')[0];
            }

            JobId.Set(context, jobInfo.JobId.ToString());
            ProcessName.Set(context, currentProcess);
            ProcessVersion.Set(context, jobInfo.ProcessVersion.ToString());
            WorkflowFilePath.Set(context, jobInfo.WorkflowFilePath.ToString());
        }
    }
}
