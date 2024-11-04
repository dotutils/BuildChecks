# DotUtils Custom BuildChecks

## UntrustedLocationCheck
It is strongly recommended not to place MSBuild project files into locations where other logic have write access to the parent folders.
That is because MSBuild (more specifically SDK common targets) hierarchically traverses folder structure for auto-importable msbuild logic.
This Check flags attempts to build from Downloads folder.

## EnvironmentVariableSecretsCheck
The UsedEnvironmentVariablesCheck is a custom MSBuild check that detects sensitive data and secrets in environment variables used during the build process. This check helps identify potential security risks by scanning environment variable values for common secrets, explicit secrets, and usernames.
The check automatically monitors environment variable access during the build process. When a secret is detected, it generates a build warning with the following information:

Secret type (SubKind)
Secret value (truncated based on verbose settings)
Location in build files (file, line, column)

Example Output
Copywarning DU0202: CommonSecret with value: 'APIKey123***' at project.props(12,5)
warning DU0202: Username with value: 'admin@comp***' at Directory.Build.props(25,10)

## UnexpectedNugetBuildLogic
TBD
