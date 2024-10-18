using Microsoft.Build.Construction;
using Microsoft.Build.Experimental.BuildCheck;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.IO;

namespace DotUtils.BuildChecks
{
    public sealed class UntrustedLocationCheck : Check
    {
        public static CheckRule SupportedRule = new CheckRule(
            "DU0201",
            "UntrustedLocation",
            "Projects should not be placed in untrusted locations",
            "Location: '{0}' cannot be fully trusted, place your projects outside of that folder (Project: {1}).",
            new CheckConfiguration() {Severity = CheckResultSeverity.Error});

        public override string FriendlyName => "DotUtils.UntrustedLocationCheck";

        public override IReadOnlyList<CheckRule> SupportedRules { get; } = new List<CheckRule>() { SupportedRule };

        public override void Initialize(ConfigurationContext configurationContext)
        {
            checkedProjects.Clear();
        }


        public override void RegisterActions(IBuildCheckRegistrationContext registrationContext)
        {
            registrationContext.RegisterEvaluatedPropertiesAction(EvaluatedPropertiesAction);
        }

        private HashSet<string> checkedProjects = new HashSet<string>();

        private void EvaluatedPropertiesAction(BuildCheckDataContext<EvaluatedPropertiesCheckData> context)
        {
            if (checkedProjects.Add(context.Data.ProjectFilePath) && context.Data.ProjectFileDirectory
                    .TrimEnd(new[] { '\\', '/' }).StartsWith(PathsHelper.Downloads))
            {
                context.ReportResult(BuildCheckResult.Create(
                    SupportedRule,
                    ElementLocation.EmptyLocation,
                    context.Data.ProjectFileDirectory,
                    context.Data.ProjectFilePath.Substring(context.Data.ProjectFileDirectory.Length + 1)));
            }
        }

        private static class PathsHelper
        {
            public static readonly string Downloads = GetDownloadsPath();

            private static string GetDownloadsPath()
            {
                return SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero);
            }

            [DllImport("shell32",
                CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
            private static extern string SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
                IntPtr hToken);
        }
    }
}
