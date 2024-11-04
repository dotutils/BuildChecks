using DotUtils.MsBuild.SensitiveDataDetector;
using Microsoft.Build.BuildCheck.Infrastructure;
using Microsoft.Build.Construction;
using Microsoft.Build.Experimental.BuildCheck;
using Microsoft.Build.SensitiveDataDetector;
using System.Collections.Generic;
using System.IO;

namespace  DotUtils.BuildChecks
{
    public sealed class ImportedProjectsSecretsCheck : Check
    {
        private EvaluationCheckScope _scope;

        private readonly List<ISensitiveDataDetector> secretsDetectors = new List<ISensitiveDataDetector>();

        public static CheckRule SupportedRule = new CheckRule(
            "DU0203",
            "SecretsDetector",
            "The check for detecting secrets in the imported projects.",
            "Detected secret: {0}",
            new CheckConfiguration());

        public override string FriendlyName => "DotUtils.ImportedProjectsSecrets";

        public override IReadOnlyList<CheckRule> SupportedRules { get; } = new List<CheckRule>() { SupportedRule };

        public override void Initialize(ConfigurationContext configurationContext)
        {
            _scope = configurationContext.CheckConfig[0].EvaluationCheckScope;
        }

        public ImportedProjectsSecretsCheck()
        {
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.CommonSecrets, false));
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.ExplicitSecrets, false));
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.Username, false));
        }

        public override void RegisterActions(IBuildCheckRegistrationContext registrationContext)
        {
            registrationContext.RegisterProjectImportedAction(ProjectImportedAction);
        }

        private void ProjectImportedAction(BuildCheckDataContext<ProjectImportedCheckData> context)
        {
            if (string.IsNullOrEmpty(context.Data.ImportedProjectFileFullPath) 
                || !CheckScopeClassifier.IsActionInObservedScope(_scope, context.Data.ImportedProjectFileFullPath, context.Data.ProjectFilePath))
            {
                return;
            }

            string content = File.ReadAllText(context.Data.ImportedProjectFileFullPath);

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            foreach (var detector in secretsDetectors)
            {
                var secrets = detector.Detect(content);
                foreach (var secret in secrets)
                {
                    foreach (var sv in secret.Value)
                    {
                        context.ReportResult(BuildCheckResult.Create(
                            SupportedRule,
                            ElementLocation.Create(context.Data.ImportedProjectFileFullPath, sv.Line, sv.Column),
                            $"{secret.Key.ToString()} with value: '{sv.Secret}'"));
                    }                   
                }
            }
        }
    }
}
