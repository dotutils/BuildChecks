# DotUtils Custom BuildChecks

## UntrustedLocationCheck
It is strongly recommended not to place MSBuild project files into locations where other logic have write access to the parent folders.
That is because MSBuild (more specifically SDK common targets) hierarchically traverses folder structure for auto-importable msbuild logic.
This Check flags attempts to build from Downloads folder.

## EnvironmentVariableSecretsCheck
The UsedEnvironmentVariablesCheck is a custom MSBuild check that detects sensitive data and secrets in environment variables used during the build process. This check helps identify potential security risks by scanning environment variable values for common secrets, explicit secrets, and usernames.
The check automatically monitors environment variable access during the build process. 

When a secret is detected, it generates a build warning with the following information:
- Secret type (SubKind)
- Secret value (truncated based on verbose settings)
- Location in build files (file, line, column)

### Example Output
`warning DU0202: CommonSecret with value: 'APIKey123***' at project.props(12,5)`

## ImportedProjectsSecretsCheck
The ImportedProjectsSecretsCheck is a custom MSBuild check that scans imported project files for sensitive data and secrets during the build process. This security-focused check examines the content of imported .props and .targets files to identify potential security risks such as common secrets, explicit secrets, and usernames embedded in the project files.

The check automatically scans project files when they are imported during the build process. It respects the configured evaluation scope to determine which imported files should be analyzed.

When a secret is detected, it generates a build warning with the following information:
- Secret type (CommonSecrets, ExplicitSecrets, or Username)
- The detected secret value
- Precise location within the imported file (file path, line, column)

### Example Output
`warning DU0203: CommonSecret with value: 'SuperSecretToken123' at imported/custom.props(15,8)`

Note: The check considers the evaluation scope configuration to determine which imported projects to analyze, helping to focus the security scanning on relevant project files within your build hierarchy.

## UnexpectedNugetBuildLogic
TBD
