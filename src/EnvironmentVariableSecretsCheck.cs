using DotUtils.MsBuild.SensitiveDataDetector;
using Microsoft.Build.BuildCheck.Infrastructure;
using Microsoft.Build.Construction;
using Microsoft.Build.Experimental.BuildCheck;
using Microsoft.Build.SensitiveDataDetector;
using Microsoft.Build.Shared;
using System;
using System.Collections.Generic;

namespace DotUtils.BuildChecks
{
    public sealed class EnvironmentVariableSecretsCheck : Check
    {
        /// <summary>
        /// Contains the list of viewed environment variables.
        /// </summary>
        private readonly HashSet<EnvironmentVariableIdentityKey> _environmentVariablesCache = new HashSet<EnvironmentVariableIdentityKey>();

        private readonly Queue<(string projectPath, BuildCheckDataContext<EnvironmentVariableCheckData>)> _buildCheckResults = new Queue<(string, BuildCheckDataContext<EnvironmentVariableCheckData>)>();

        private readonly List<ISensitiveDataDetector> secretsDetectors = new List<ISensitiveDataDetector>();

        private EvaluationCheckScope _scope;

        private const string RuleId = "DU0202";

        private const string VerboseOutputKey = "allow_displaying_property_value";

        public static CheckRule SupportedRule = new CheckRule(
            RuleId,
            "SecretsDetector",
            "The check for detecting secrets in the used environment variables.",
            "Detected secret: {0}",
            new CheckConfiguration());

        public override string FriendlyName => "DotUtils.EnvironmentVariableSecretsCheck";

        public bool IsVerbose { get; set; }

        public override IReadOnlyList<CheckRule> SupportedRules { get; } = new List<CheckRule>() { SupportedRule };

        // Custom configuration for the check
        public override void Initialize(ConfigurationContext configurationContext)
        {
            _scope = configurationContext.CheckConfig[0].EvaluationCheckScope;

            foreach (CustomConfigurationData customConfigurationData in configurationContext.CustomConfigurationData)
            {
                if (customConfigurationData.RuleId.Equals(RuleId, StringComparison.InvariantCultureIgnoreCase)
                    && (customConfigurationData.ConfigurationData?.TryGetValue(VerboseOutputKey, out string configVal) ?? false))
                {
                    IsVerbose = !string.IsNullOrEmpty(configVal) && bool.Parse(configVal);
                }
            }
        }

        public EnvironmentVariableSecretsCheck()
        {
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.CommonSecrets, false));
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.ExplicitSecrets, false));
            secretsDetectors.Add(SensitiveDataDetectorFactory.GetSecretsDetector(SensitiveDataKind.Username, false));
        }

        public override void RegisterActions(IBuildCheckRegistrationContext registrationContext)
        {
            registrationContext.RegisterEnvironmentVariableReadAction(EnvironmentVariableUsedAction);
        }

        private void EnvironmentVariableUsedAction(BuildCheckDataContext<EnvironmentVariableCheckData> context)
        {
            // Scope information is available after evaluation of the project file. If it is not ready, we will report the check later.
            if (!CheckScopeClassifier.IsScopingReady(_scope))
            {
                _buildCheckResults.Enqueue((context.Data.ProjectFilePath, context));
            }
            else if (CheckScopeClassifier.IsActionInObservedScope(_scope, context.Data.EnvironmentVariableLocation.File, context.Data.ProjectFilePath))
            {

                EnvironmentVariableIdentityKey identityKey = new(context.Data.EnvironmentVariableName, context.Data.EnvironmentVariableLocation);
                if (!_environmentVariablesCache.Contains(identityKey))
                {
                    foreach (var detector in secretsDetectors)
                    {
                        var secrets = detector.Detect(context.Data.EnvironmentVariableValue);
                        foreach (var secret in secrets)
                        {
                            foreach (var sv in secret.Value)
                            {
                                context.ReportResult(BuildCheckResult.Create(
                                    SupportedRule,
                                    context.Data.EnvironmentVariableLocation,
                                    $"{sv.SubKind} with value: '{(IsVerbose ? sv.Secret : sv.Secret.Substring(0, 3) + "***")}'"));
                            }
                        }
                    }

                    _environmentVariablesCache.Add(identityKey);
                }
            }
        }

        internal class EnvironmentVariableIdentityKey(string environmentVariableName, IMSBuildElementLocation location) : IEquatable<EnvironmentVariableIdentityKey>
        {
            public string EnvironmentVariableName { get; } = environmentVariableName;

            public IMSBuildElementLocation Location { get; } = location;

            public override bool Equals(object? obj) => Equals(obj as EnvironmentVariableIdentityKey);

            public bool Equals(EnvironmentVariableIdentityKey? other) =>
                other != null &&
                EnvironmentVariableName == other.EnvironmentVariableName &&
                Location.File == other.Location.File &&
                Location.Line == other.Location.Line &&
                Location.Column == other.Location.Column;

            public override int GetHashCode()
            {
                int hashCode = 17;
                hashCode = hashCode * 31 + (Location.File != null ? Location.File.GetHashCode() : 0);
                hashCode = hashCode * 31 + Location.Line.GetHashCode();
                hashCode = hashCode * 31 + Location.Column.GetHashCode();

                return hashCode;
            }
        }
    }
}
