using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaskela.PowerAutomateAssistant.TrueFieldChange
{
    public class PreUpdate : IPlugin
    {
        public string SecureString { get; private set; }
        public string UnsecureString { get; private set; }

        public PreUpdate(string unsecureString, string secureString)
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

            tracing.Trace($"Starting TrueFieldChange / PreUpdate / {context.PrimaryEntityName}");
            if (context.Stage != 20)
            {
                tracing.Trace("Incorrect stage: Must be Pre-Operation");
                throw new Exception("The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreUpdate must be configured on the Pre-Operation stage");
            }
            if (context.MessageName != "Update")
            {
                tracing.Trace("Incorrect message: Must be Update");
                throw new Exception("The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreUpdate must be configured on the Update message");
            }
            if (context.PreEntityImages == null || context.PreEntityImages.Count == 0)
            {
                tracing.Trace("Missing the entity Pre Image");
                throw new Exception("The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreUpdate must include the Pre Image");
            }

            tracing.Trace($"Retrieving Unsecured configuration: {this.UnsecureString}");
            if (String.IsNullOrWhiteSpace(this.UnsecureString))
            {
                tracing.Trace("Unsecured configuration not defined");
                throw new Exception($"The plugin Kaskela.PowerAutomateAssistant.TrueFieldChange.PreUpdate does not have a defined Unsecured Configuration: {context.PrimaryEntityName}");
            }

            var configuration = TrueFieldConfiguration.Deserialize(this.UnsecureString);
            tracing.Trace($"Configuration Parsed. Text Field Name: {configuration.TextFieldName}; Include False Changes: {configuration.IncludeGhostChanges}; Trigger Fields: {configuration.TriggerFields.Count()}");

            var preEntity = context.PreEntityImages.First().Value;
            var target = (Entity)context.InputParameters["Target"];
            var merged = this.MergeEntities(preEntity, target);

            List<string> changedFields = new List<string>();
            foreach (var configuredField in configuration.TriggerFields.Select(t => t.ToLower()))
            {
                tracing.Trace($"Evaluating {configuredField}");

                // Ghost updates will be in the target entity so we don't need to change any further
                if (configuration.IncludeGhostChanges && target.Contains(configuredField))
                {
                    changedFields.Add(configuredField);
                }
                else
                {
                    if ((preEntity.Contains(configuredField) && preEntity[configuredField] != null) && (!merged.Contains(configuredField) || merged[configuredField] == null))
                    {
                        tracing.Trace($"Field has been cleared");
                        changedFields.Add(configuredField);
                    }
                    if ((!preEntity.Contains(configuredField) || preEntity[configuredField] == null) && (merged.Contains(configuredField) && merged[configuredField] != null))
                    {
                        tracing.Trace($"Field has been set");
                        changedFields.Add(configuredField);
                    }
                    else if (preEntity.Contains(configuredField) && preEntity[configuredField] != null && merged.Contains(configuredField) && merged[configuredField] != null)
                    {
                        var preValue = preEntity[configuredField];
                        var postValue = merged[configuredField];
                        if (preValue is OptionSetValue) // Single Select Option Set
                        {
                            if (((OptionSetValue)preValue).Value != ((OptionSetValue)postValue).Value)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is OptionSetValueCollection) // Multi Select Option Set
                        {
                            var preValues = (preValue as OptionSetValueCollection).Select(o => o.Value).ToList();
                            var postValues = (postValue as OptionSetValueCollection).Select(o => o.Value).ToList();

                            if (preValues.Except(postValues).Any() || postValues.Except(preValues).Any())
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is string) // Multi-line, Single Line
                        {
                            if (((string)preValue).Equals((string)postValue) == false)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is EntityReference) // Lookup, Customer, Owner
                        {
                            if (((EntityReference)preValue).LogicalName != ((EntityReference)postValue).LogicalName ||
                                ((EntityReference)preValue).Id != ((EntityReference)postValue).Id)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is bool) // Two Option
                        {
                            if ((bool)preValue != (bool)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is DateTime) // Date, DateTime
                        {
                            if ((DateTime)preValue != (DateTime)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is decimal) // Decimal
                        {
                            if ((decimal)preValue != (decimal)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is double) // Float
                        {
                            if ((double)preValue != (double)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is Money) // Money
                        {
                            if (((Money)preValue).Value != ((Money)postValue).Value)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is int) // Whole Number
                        {
                            if ((int)preValue != (int)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else if (preValue is Guid) //
                        {
                            if ((Guid)preValue != (Guid)postValue)
                            {
                                tracing.Trace($"Field has changed");
                                changedFields.Add(configuredField);
                            }
                        }
                        else
                        {
                            tracing.Trace($"This field type isn't supported! {preValue.GetType()}");
                        }
                    }
                }
            }

            tracing.Trace($"Total modified fields: {changedFields}");
            if (changedFields.Any())
            {
                target[configuration.TextFieldName.ToLower()] = String.Join(",", changedFields.OrderBy(c => c));
            }
        }


        protected Entity MergeEntities(Entity preImage, Entity target)
        {
            Entity entity = new Entity(preImage.LogicalName, preImage.Id);

            foreach (var attributeKey in target.Attributes.Keys.Union(preImage.Attributes.Keys).Distinct())
            {
                if (target.Contains(attributeKey))
                {
                    entity[attributeKey] = target[attributeKey];
                }
                else
                {
                    entity[attributeKey] = preImage[attributeKey];
                }
            }
            return entity;
        }
    }
}
