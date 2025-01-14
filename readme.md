# <img src='/src/icon.png' height='30px'> Alias

[![Build status](https://ci.appveyor.com/api/projects/status/s3agb6fiax7pgwls/branch/main?svg=true)](https://ci.appveyor.com/project/SimonCropp/dotnet-assembly-alias)
[![NuGet Status](https://img.shields.io/nuget/v/Alias.svg?label=Alias%20nuget)](https://www.nuget.org/packages/Alias/)
[![NuGet Status](https://img.shields.io/nuget/v/Alias.Lib.svg?label=Alias.Lib%20nuget)](https://www.nuget.org/packages/Alias.Lib/)

Rename assemblies and fixes references. Designed as an alternative to [Costura](https://github.com/Fody/Costura), [ILMerge](https://github.com/dotnet/ILMerge), and [ILRepack](https://github.com/gluck/il-repack).

Designed to mitigate scenarios where an assembly is run in a plugin scenario. For example Unity extensions, MSBuild tasks, or SharePoint extensions. In these scenarios an assembly, and all its references, are loaded into a shared AppDomain. So dependencies operate as "first on wins". So, for example, if two addins assemblies use different versions of Newtonsoft, the first addin that is loaded defines what version of Newtonsoft is used by all subsequent addins assemblies.

This project works around this problem by renaming references and preventing name conflicts.


## dotnet tool

https://www.nuget.org/packages/Alias/

**[.net 6](https://dotnet.microsoft.com/download/dotnet/6.0) or higher is required to run this tool.**

For a given directory and a subset of assemblies:

 * Changes the assembly name of each "alias" assembly.
 * Renames "alias" assemblies on disk.
 * For all assemblies, fixes the references to point to the new alias assemblies.


### Installation

Ensure [dotnet CLI is installed](https://docs.microsoft.com/en-us/dotnet/core/tools/).

Install [Alias](https://nuget.org/packages/Alias/)

```ps
dotnet tool install --global Alias
```


### Usage

```ps
assemblyalias --target-directory "C:/Code/TargetDirectory"
              --suffix _Alias
              --assemblies-to-alias "Microsoft*;System*;EmptyFiles"
              --assemblies-to-exclude "e_sqlite*"
```


### Arguments


#### Target Directory

`-t` or `--target-directory`

Optional. If no directory is passed the current directory will be used.


#### Internalize

`-i` or `--internalize`

Optional. To internalize all types in the aliased assemblies. Defaults to false.


#### Prefix/Suffix

Either a prefix or suffix must be defined.


##### Prefix

`-p` or `--prefix`

The prefix to use when renaming assemblies.


##### Suffix

`-s` or `--suffix`

The suffix to use when renaming assemblies.


#### Assemblies to alias

`-a` or `--assemblies-to-alias`

Required. A semi-colon separated list of assembly names to alias. Names ending in `*` are treated as wildcards.


#### Assemblies to exclude

`-e` or `--assemblies-to-exclude`

Optional. A semi-colon separated list of assembly names to exclude. Names ending in `*` are treated as wildcards.


#### Key

`-k` or `--key`

Path to an snk file.

Optional. If no key is passed, strong naming will be removed from all assemblies.


#### References

`-r` or `--references`

Optional. A semi-colon separated list of paths to reference files.


#### Reference File

`--reference-file`

Optional. A path to a file containing references file paths. On file path per line.


##### Default Reference File

By default the target directory will be scanned for a reference file named `alias-references.txt`

It can be helpful to extract reference during a build using msbuild and write them to a file accessible to Alias:

<!-- snippet: WriteReferenceForAlias -->
<a id='snippet-writereferenceforalias'></a>
```csproj
<Target Name="WriteReferenceForAlias" AfterTargets="AfterCompile">
  <ItemGroup>
    <ReferenceForAlias Include="@(ReferencePath)" Condition="'%(FileName)' == 'CommandLine'" />
  </ItemGroup>
  <WriteLinesToFile File="$(TargetDir)/alias-references.txt" Lines="%(ReferenceForAlias.FullPath)" Overwrite="true" />
</Target>
```
<sup><a href='/src/SampleApp/SampleApp.csproj#L19-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-writereferenceforalias' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
