using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CheckConsecutiveStudioRuns
{

    public class RuleRepository : IRegisterAnalyzerConfiguration
    {

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            if (!workflowAnalyzerConfigService.HasFeature("WorkflowAnalyzerV4"))
            {
                return;
            }

            var newRule = new Rule<IProjectModel>(Resource.RequirePublishRuleName, Resource.RuleId, InspectNumberOfConsecutiveRuns)
            {
                DefaultErrorLevel = System.Diagnostics.TraceLevel.Error,
                ApplicableScopes = new List<string> { RuleConstants.DevelopmentRuleConstant, RuleConstants.BusinessRuleConstant, RuleConstants.TestAutomationRuleConstant },
                RecommendationMessage = Resource.RuleRecommendation,
                DocumentationLink = "https://marketplace.uipath.com/"
            };

            newRule.Parameters.Add(Resource.RequirePublishAfterParameterKey, new Parameter()
            {
                DefaultValue = Resource.RequirePublishAfterParameterDefault,
                Key = Resource.RequirePublishAfterParameterKey,
                LocalizedDisplayName = Resource.RequirePublishAfterParameterName
            });

            newRule.Parameters.Add(Resource.WarningMessageParameterKey, new Parameter()
            {
                DefaultValue = Resource.WarningMessageParameterDefault,
                Key = Resource.WarningMessageParameterKey,
                LocalizedDisplayName = Resource.WarningMessageParameterName
            });

            newRule.Parameters.Add(Resource.UrlParameterKey, new Parameter()
            {
                DefaultValue = Resource.UrlParameterDefault,
                Key = Resource.UrlParameterKey,
                LocalizedDisplayName = Resource.UrlParameterName
            });

            workflowAnalyzerConfigService.AddRule(newRule);
        }

       
        public  InspectionResult InspectNumberOfConsecutiveRuns(IProjectModel projectToInspect, Rule configuredRule)
        {
            var maxNumberOfRunsString = configuredRule.Parameters[Resource.RequirePublishAfterParameterKey]?.Value;
            var isMaxNumberStringValidInt = int.TryParse(maxNumberOfRunsString, out int maxNumberOfRuns);
            configuredRule.DefaultErrorLevel = TraceLevel.Error;
            configuredRule.DefaultIsEnabled = true;
            var warningMessage = configuredRule.Parameters[Resource.WarningMessageParameterKey]?.Value;

            var urlHelper = configuredRule.Parameters[Resource.UrlParameterKey]?.Value;

            if (!isMaxNumberStringValidInt)
            {
                return new InspectionResult()
                    {
                        HasErrors = true,
                        ErrorLevel = configuredRule.ErrorLevel,
                        RecommendationMessage = Resource.RecommendationInputValidationErrorMessage,
                        InspectionMessages = new List<InspectionMessage>
                        {
                            new InspectionMessage()
                            {
                                Message = Resource.RequireAfterPublishInvalidInput
                            }
                        }
                    };
            }

            var persistanceFilePath = Utils.GetPathToPersistanceFile((projectToInspect as IInspectionObject).DisplayName, projectToInspect.ProjectFilePath);
            // check last run time to work around the StudioX bug of running the WFA twice.
            if (Utils.WasLastRunTooRecent(persistanceFilePath))
            {               
                return new InspectionResult() { HasErrors = false };
            }

            PersistanceInfoModel[] existingPersistanceInfoArray = Utils.ReadPersistanceFileArray(persistanceFilePath);

            string projectFilePath = projectToInspect.ProjectFilePath;
            if (projectFilePath=="project.json")
            {
                if (Utils.HasProperty(projectToInspect, "Directory"))
                {
                    PropertyInfo propertyInfo = projectToInspect.GetType().GetProperty("Directory");
                    projectFilePath= propertyInfo.GetValue(projectToInspect).ToString() + "\\project.json";
                }                
            }
            PersistanceInfoModel[] currentpersistanceInfoModels = Utils.GenerateHashFile(projectFilePath);
            if (existingPersistanceInfoArray == null)
            {
                Utils.WritePersistanceFile(persistanceFilePath, currentpersistanceInfoModels);
                return new InspectionResult() { HasErrors = false };
            }

            //Project.json hash will always be on index 0;
            PersistanceInfoModel projectPersistanceInfo = existingPersistanceInfoArray[0];

            #region Check project.json file for any changes at project level
            PersistanceInfoModel projectCurrentInfo = currentpersistanceInfoModels[0];
            if (projectPersistanceInfo.Hash!=projectCurrentInfo.Hash)
            {
                projectCurrentInfo.Count = 1;
                Utils.WritePersistanceFile(persistanceFilePath, currentpersistanceInfoModels);
                return new InspectionResult() { HasErrors = false };
            }
            #endregion

            #region Check XAML Files
            bool fileChanged = false;
            foreach (var item in currentpersistanceInfoModels)
            {
                PersistanceInfoModel fileModel = Utils.GetFileDetails(item.FileName, existingPersistanceInfoArray);
                if (fileModel == null || fileModel.Hash!=item.Hash)
                {
                    fileChanged = true;
                    break;
                }
            }
            #endregion

            #region Final block
            if (fileChanged)
            {
                projectCurrentInfo.Count = 1;
                Utils.WritePersistanceFile(persistanceFilePath, currentpersistanceInfoModels);
                return new InspectionResult() { HasErrors = false };
            }
            else
            {                
                if (projectPersistanceInfo.Count>maxNumberOfRuns)
                {
                    using (Form1 form = new Form1(warningMessage) { TopMost = true })
                    {
                        var hwnd = ProcessHelper.GetParentWindow();
                        var result = form.ShowDialog(hwnd);
                    }

                    var messageList = new List<string>();
                    messageList.Add(warningMessage);

                    return new InspectionResult()
                    {                       
                        HasErrors = true,
                        ErrorLevel = configuredRule.ErrorLevel,
                        DocumentationLink = urlHelper,
                        RecommendationMessage = warningMessage,
                        Messages = messageList
                    }; 
                }
            }
            projectCurrentInfo.Count = projectPersistanceInfo.Count + 1;
            Utils.WritePersistanceFile(persistanceFilePath, currentpersistanceInfoModels);
            return new InspectionResult() { HasErrors = false };
            #endregion           
        }        
    }
}
