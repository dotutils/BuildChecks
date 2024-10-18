# DotUtils Custom BuildChecks

## UntrustedLocationCheck
It is strongly recommended not to place MSBuild project files into locations where other logic have write access to the parent folders.
That is because MSBuild (more specifically SDK common targets) hierarchically traverses folder structure for auto-importable msbuild logic.
This Check flags attempts to build from Downloads folder.

## UnexpectedNugetBuildLogic
TBD
