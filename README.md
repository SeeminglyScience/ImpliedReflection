<h1 align="center">ImpliedReflection</h1>

<p align="center">
    <sub>
      Access non-public types and type members as if they were public.
    </sub>
    <br /><br />
    <a title="Commits" href="https://github.com/SeeminglyScience/ImpliedReflection/commits/master">
        <img alt="Build Status" src="https://github.com/SeeminglyScience/ImpliedReflection/workflows/build/badge.svg" />
    </a>
    <a title="ImpliedReflection on PowerShell Gallery" href="https://www.powershellgallery.com/packages/ImpliedReflection">
        <img alt="PowerShell Gallery Version (including pre-releases)" src="https://img.shields.io/powershellgallery/v/ImpliedReflection?include_prereleases&label=gallery">
    </a>
    <a title="LICENSE" href="https://github.com/SeeminglyScience/ImpliedReflection/blob/master/LICENSE">
         <img alt="GitHub" src="https://img.shields.io/github/license/SeeminglyScience/ImpliedReflection">
    </a>
</p>


**IMPORTANT:** This project operates entirely on an absurd amount of implementation detail. This is not a supported product, may break at any time, and should not be used in production environments. This is meant to be used only for exploration and/or interactive troubleshooting.

## Features

- Access any type, field, property, method, constructor, or event regardless of accessibility
- Binding and resolution is done no differently from public types and type members
- Member binding is done automatically by hooking directly into various parts of the PowerShell engine

## Installation

### Gallery

```powershell
Install-Module ImpliedReflection -Scope CurrentUser
```

### Source

```powershell
git clone 'https://github.com/SeeminglyScience/ImpliedReflection.git'
Set-Location .\ImpliedReflection
Invoke-Build -Task Install
```
## Usage


```powershell
using namespace System.Management.Automation

Enable-ImpliedReflection -YesIKnowIShouldNotDoThis

$sbd = [CompiledScriptBlockData]::new(
    '"hi!"',
    $false)

$sb = [scriptblock]::new($sbd)
$optimized = $true
$action = $sb.GetCodeToInvoke([ref] $optimized, [ScriptBlockClauseToInvoke]::End)

$scope = $null
try {
    $locals = $sb.MakeLocalsTuple($true)
    $scope = $ExecutionContext._context.EngineSessionState.NewScope($false)
    $scope.LocalsTuple = $locals
    $locals.SetAutomaticVariable(
        [AutomaticVariable]::MyInvocation,
        $MyInvocation,
        $ExecutionContext._context)

    $context = [Language.FunctionContext]@{
        _executionContext = $ExecutionContext._context
        _outputPipe = $ExecutionContext._context.CurrentCommandProcessor.CommandRuntime.OutputPipe
        _localsTuple = $scope.LocalsTuple
        _scriptBlock = $sb
        _file = $null
        _debuggerHidden = $false
        _debuggerStepThrough = $false
        _sequencePoints = $sbd.SequencePoints
    }

    $Host.Runspace.Debugger.EnterScriptFunction($context)
    $action.Invoke($context)
} finally {
    if ($null -ne $scope) {
        $ExecutionContext._context.EngineSessionState.RemoveScope($scope)
    }
}
```

Goes the long way around to invoke a scriptblock of `"hi!"`. There's no point to do this, but it shows off all the different things that work with ImpliedReflection.

### Explore properties and fields

```powershell
Enable-ImpliedReflection -YesIKnowIShouldNotDoThis
$ExecutionContext._context.HelpSystem
<#
ExecutionContext      : System.Management.Automation.ExecutionContext
LastErrors            : {}
LastHelpCategory      : None
VerboseHelpErrors     : False
HelpProviders         : {System.Management.Automation.AliasHelpProvider,
                        System.Management.Automation.ScriptCommandHelpProvider,
                        System.Management.Automation.CommandHelpProvider,
                        System.Management.Automation.ProviderHelpProvider...}
HelpErrorTracer       : System.Management.Automation.HelpErrorTracer
ScriptBlockTokenCache : {}
_executionContext     : System.Management.Automation.ExecutionContext
OnProgress            :
_lastErrors           : {}
_lastHelpCategory     : None
_verboseHelpErrors    : False
_searchPaths          :
_helpProviders        : {System.Management.Automation.AliasHelpProvider,
                        System.Management.Automation.ScriptCommandHelpProvider,
                        System.Management.Automation.CommandHelpProvider,
                        System.Management.Automation.ProviderHelpProvider...}
_helpErrorTracer      : System.Management.Automation.HelpErrorTracer
_culture              :
#>
```

### Invoke private methods

```powershell
$scriptblock = { 'Test ScriptBlock' }
$scriptblock.InvokeUsingCmdlet
<#
OverloadDefinitions
-------------------
void InvokeUsingCmdlet(System.Management.Automation.Cmdlet contextCmdlet, bool useLocalScope,
System.Management.Automation.ScriptBlock+ErrorHandlingBehavior, System.Management.Automation, Version=3.0.0.0,
Culture=neutral, PublicKeyToken=31bf3856ad364e35 errorHandlingBehavior, System.Object dollarUnder, System.Object
input, System.Object scriptThis, System.Object[] args)
#>
$scriptblock.InvokeUsingCmdlet($PSCmdlet, $true, 'SwallowErrors', $_, $input, $this, $args)
```

### Explore static members

```powershell
[scriptblock]::delegateTable
<#
Keys      : { $args[0].Name }
Values    : {System.Collections.Concurrent.ConcurrentDictionary`2[System.Type,System.Delegate]}
_buckets  : {-1, -1, 11, 7...}
_entries  : {System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry[System.Management.Automation.ScriptBlock,Syst
            em.Collections.Concurrent.ConcurrentDictionary`2[System.Type,System.Delegate]], System.Runtime.CompilerServ
            ices.ConditionalWeakTable`2+Entry[System.Management.Automation.ScriptBlock,System.Collections.Concurrent.Co
            ncurrentDictionary`2[System.Type,System.Delegate]], System.Runtime.CompilerServices.ConditionalWeakTable`2+
            Entry[System.Management.Automation.ScriptBlock,System.Collections.Concurrent.ConcurrentDictionary`2[System.
            Type,System.Delegate]], System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry[System.Management.Auto
            mation.ScriptBlock,System.Collections.Concurrent.ConcurrentDictionary`2[System.Type,System.Delegate]]...}
_freeList : 3
_lock     : System.Object
_invalid  : False
#>
[System.Management.Automation.Utils]::IsAdministrator()
<#
False
#>
[psmoduleinfo] | Get-Member -Static
<#
   TypeName: System.Management.Automation.PSModuleInfo

Name                                          MemberType Definition
----                                          ---------- ----------
AddModuleToList                               Method     static void AddModuleToList(psmoduleinfo module, System.Col...
AddToAppDomainLevelModuleCache                Method     static void AddToAppDomainLevelModuleCache(string moduleNam...
ClearAppDomainLevelModulePathCache            Method     static void ClearAppDomainLevelModulePathCache()
Equals                                        Method     static bool Equals(System.Object objA, System.Object objB)
GetUriFromString                              Method     static uri GetUriFromString(string uriString)
new                                           Method     psmoduleinfo new(bool linkToGlobal), psmoduleinfo new(scrip...
ReferenceEquals                               Method     static bool ReferenceEquals(System.Object objA, System.Obje...
RemoveFromAppDomainLevelCache                 Method     static bool RemoveFromAppDomainLevelCache(string moduleName)
ResolveUsingAppDomainLevelModuleCache         Method     static string ResolveUsingAppDomainLevelModuleCache(string ...
SetDefaultDynamicNameAndPath                  Method     static void SetDefaultDynamicNameAndPath(psmoduleinfo module)
<UseAppDomainLevelModuleCache>k__BackingField Property   static bool <UseAppDomainLevelModuleCache>k__BackingField {...
DynamicModulePrefixString                     Property   static string DynamicModulePrefixString {get;set;}
EmptyTypeDefinitionDictionary                 Property   static System.Collections.ObjectModel.ReadOnlyDictionary[st...
ScriptModuleExtensions                        Property   static System.Collections.Generic.HashSet[string] ScriptMod...
UseAppDomainLevelModuleCache                  Property   static bool UseAppDomainLevelModuleCache {get;set;}
_appdomainModulePathCache                     Property   static System.Collections.Concurrent.ConcurrentDictionary[s...
_builtinVariables                             Property   static string[] _builtinVariables {get;set;}
#>
```

## Contributions Welcome!

We would love to incorporate community contributions into this project.  If you would like to
contribute code, documentation, tests, or bug reports, please read our [Contribution Guide](https://github.com/SeeminglyScience/ImpliedReflection/tree/master/docs/CONTRIBUTING.md) to learn more.

## Special Thanks

- [aelij/IgnoresAccessChecksToGenerator](https://github.com/aelij/IgnoresAccessChecksToGenerator) - A modified (to handle different outputs based on target framework) copy of their source is included in this project
