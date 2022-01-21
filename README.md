![Failed to load build status. Maybe the repo name must be updated?](https://github.com/jmaarleveld/Faultify/actions/workflows/dotnet.yml/badge.svg)

<h1 align="center"><img width="500" src="docs/Windesheim_docs/full-logo.png" /></h1>

## Byte Code Dotnet Mutation Utility
Faultify provides a quick simple way to realize dotnet mutation testing at the byte code level. 
It imitates the bad programmer by deliberately introducing errors. 
A test is assumed to fail after an introduced mutation. If it succeeds, the test is likely to be error-prone and problematic.

Faultify is targeted at .NET Core 3.0+ projects and does not support Xamarin nor .NET Framework.

*disclaimer: faultify is just released and bugs can be expected, please open a issue if you get any.*

### Getting Started

**Commandline Options**

```
  -i, --inputProject                  Required. The path pointing to the test project project file.
  -r, --reportPath                    The path were the report will be saved.
  -f, --reportFormat                  (Default: json) Type of report to be generated, options: 'pdf', 'html', 'json'
  -l, --mutationLevel                 (Default: Detailed) The mutation level indicating the test depth.
  -t, --testHost                      (Default: DotnetTest) The name of the test host framework.
  -d, --timeOut                       (Default: 0) Time out in seconds for the mutations
  -g, --excludeMutationGroups         Mutation groups to be excluded
  -s, --excludeMutationSingles        Individual mutation ids to be excluded
  -e, --excludeMutationSinglesFile    (Default: NoFile) Path to a json file detailing individual mutations to be
                                      excluded

  --help                              Display this help screen.
  --version                           Display version information.
```

**Install / Run**

To install and run Faultify on your machine download the latest release and run the following command

```
path/to/faultify.exe -i path/to/target/project/Tests/Tests.csproj [OPTIONS]
```

Faultify allows the user to set up custom exception rules for mutations. For more information on this, see [Excluding Mutations](excludeMutations.md)

## Application Preview
<img src="docs/Windesheim_docs/application-overview.PNG" alt="Screenshot of Faultify" width="600"/>

## Copyright

This repository is a fork from commit [8ef5d9f](https://github.com/Faultify/Faultify/commit/8ef5d9f8d830d263aecb173732f6df82f0bc11af) of the repository [Faultify/Faultify](https://github.com/Faultify/Faultify/)
