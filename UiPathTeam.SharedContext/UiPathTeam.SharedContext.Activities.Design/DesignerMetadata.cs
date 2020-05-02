using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using UiPathTeam.SharedContext.Activities.Design.Designers;
using UiPathTeam.SharedContext.Activities.Design.Properties;

namespace UiPathTeam.SharedContext.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(ClientScopeActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(ClientScopeActivity), new DesignerAttribute(typeof(ClientScopeActivityDesigner)));
            builder.AddCustomAttributes(typeof(ClientScopeActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetProcessInfoActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetProcessInfoActivity), new DesignerAttribute(typeof(GetProcessInfoActivityDesigner)));
            builder.AddCustomAttributes(typeof(GetProcessInfoActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(NamedPipeTriggerV2), categoryAttribute);
            builder.AddCustomAttributes(typeof(NamedPipeTriggerV2), new DesignerAttribute(typeof(NamedPipeTriggerV2Designer)));
            builder.AddCustomAttributes(typeof(NamedPipeTriggerV2), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(ServerScopeActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(ServerScopeActivity), new DesignerAttribute(typeof(ServerScopeActivityDesigner)));
            builder.AddCustomAttributes(typeof(ServerScopeActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetVariableActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetVariableActivity), new DesignerAttribute(typeof(GetVariableActivityDesigner)));
            builder.AddCustomAttributes(typeof(GetVariableActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(SetVariableActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(SetVariableActivity), new DesignerAttribute(typeof(SetVariableActivityDesigner)));
            builder.AddCustomAttributes(typeof(SetVariableActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(SendMessageActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(SendMessageActivity), new DesignerAttribute(typeof(SendMessageActivityDesigner)));
            builder.AddCustomAttributes(typeof(SendMessageActivity), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(ReceiveMessageActivity), categoryAttribute);
            builder.AddCustomAttributes(typeof(ReceiveMessageActivity), new DesignerAttribute(typeof(ReceiveMessageActivityDesigner)));
            builder.AddCustomAttributes(typeof(ReceiveMessageActivity), new HelpKeywordAttribute(""));

            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
