using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaskela.PowerAutomateAssistant.TrueFieldChange
{
    public class PreCreate : IPlugin
    {
        public string SecureString { get; private set; }
        public string UnsecureString { get; private set; }

        public PreCreate(string unsecureString, string secureString)
        {
            this.SecureString = secureString;
            this.UnsecureString = unsecureString;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracing.Trace($"Starting TrueFieldChange / PreCreate / {context.PrimaryEntityName}");
            if (context.Stage != 20)
            {
                tracing.Trace("Incorrect stage: Must be Pre-Operation");
                throw new Exception("The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreCreate must be configured on the Pre-Operation stage");
            }
            if (context.MessageName != "Create")
            {
                tracing.Trace("Incorrect message: Must be Create");
                throw new Exception("The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreCreate must be configured on the Create message");
            }
            tracing.Trace($"Retrieving Unsecured configuration: {this.UnsecureString}");
            if (String.IsNullOrWhiteSpace(this.UnsecureString))
            {
                tracing.Trace("Unsecured configuration not defined");
                throw new Exception($"The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreCreate does not have a defined Unsecured Configuration: {context.PrimaryEntityName}");
            }

            var configuration = TrueFieldConfiguration.Deserialize(this.UnsecureString);
            tracing.Trace($"Configuration Parsed. Text Field Name: {configuration.TextFieldName}; Include False Changes: {configuration.IncludeGhostChanges}; Trigger Fields: {configuration.TriggerFields.Count()}");

            var target = (Entity)context.InputParameters["Target"];

            List<string> changedFields = new List<string>();
            foreach (var configuredField in configuration.TriggerFields.Select(t => t.ToLower()))
            {
                tracing.Trace($"Evaluating {configuredField}");
                if ((configuration.IncludeGhostChanges && target.Contains(configuredField)) ||
                    (!configuration.IncludeGhostChanges && target.Contains(configuredField) && target[configuredField] != null))
                {
                    tracing.Trace($"Field has changed");
                    changedFields.Add(configuredField);
                }
            }

            tracing.Trace($"Total modified fields: {changedFields}");
            if (changedFields.Any())
            {
                target[configuration.TextFieldName.ToLower()] = String.Join(",", changedFields.OrderBy(c => c));
            }
        }
    }
}
