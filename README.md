# ImpliedReflection

ImpliedReflection is a PowerShell module for exploring non-public properties and methods of objects as if they were public.

This project adheres to the Contributor Covenant [code of conduct](https://github.com/SeeminglyScience/ImpliedReflection/tree/master/docs/CODE_OF_CONDUCT.md).
By participating, you are expected to uphold this code. Please report unacceptable behavior to seeminglyscience@gmail.com.

## Features

- All members are bound to the object the **same way public members are** by the PowerShell engine.
- Supports any parameter types (including ByRef, Pointer, etc) that the PowerShell engine can handle.
- Member binding is done automatically by hooking into the PowerShell engine's binder.

## Known Issues

- Non-public static members will only be available once either:
    1. A `System.Type` object representing the type has been outputted
    1. An instance of the type has been outputted
  There's only an instance delegate in the engine, so static still depends on `Out-Default`

- Non-public constructors are available via an added `_ctor` static method

- Family (aka `protected`) properties will throw an exception stating that the property is
  write only.  This is due to explicit checks for family properties in the PowerShell's binder. You can
  get around it by calling the getter method instead (e.g. instead of `$Host.Runspace.RunningPipelines` try `$Host.Runpsace.get_RunningPipelines()`)

## Documentation

Check out our **[documentation](https://github.com/SeeminglyScience/ImpliedReflection/tree/master/docs/en-US/ImpliedReflection.md)** for information about how to use this project.

## Demos

![new-member-binding](https://user-images.githubusercontent.com/24977523/45323875-79ada380-b51a-11e8-95d0-15e605b5eb4a.gif)

![implied-reflection-demo](https://user-images.githubusercontent.com/24977523/28750154-28fda216-74af-11e7-8629-8ada279e860e.gif)


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

### Explore properties and fields

```powershell
Enable-ImpliedReflection -Force
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
# For static binding either the type (like below) or an instance of the type needs to hit
# Out-Default first.
[scriptblock]
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
[ref].Assembly.GetType('System.Management.Automation.Utils')
[ref].Assembly.GetType('System.Management.Automation.Utils')::IsAdministrator()
<#
False
#>
[psmoduleinfo]
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
