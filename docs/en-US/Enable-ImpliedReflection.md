---
external help file: ImpliedReflection-help.xml
online version: https://github.com/SeeminglyScience/ImpliedReflection/blob/master/docs/en-US/Enable-ImpliedReflection.md
schema: 2.0.0
---

# Enable-ImpliedReflection

## SYNOPSIS

Enable the binding of non public members to all objects outputted.

## SYNTAX

```powershell
Enable-ImpliedReflection [-Force]
```

## DESCRIPTION

The Enable-ImpliedReflection function replaces the Out-Default cmdlet with a proxy function that invokes Add-PrivateMember on every object outputted to the console.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
Enable-ImpliedReflection -Force
$ExecutionContext

    _context       : System.Management.Automation.ExecutionContext
    _host          : System.Management.Automation.Internal.Host.InternalHost
    _invokeCommand : System.Management.Automation.CommandInvocationIntrinsics
    Host           : System.Management.Automation.Internal.Host.InternalHost
    Events         : System.Management.Automation.PSLocalEventManager
    InvokeProvider : System.Management.Automation.ProviderIntrinsics
    SessionState   : System.Management.Automation.SessionState
    InvokeCommand  : System.Management.Automation.CommandInvocationIntrinsics

$ExecutionContext._context

    CurrentExceptionBeingHandled                                  :
    PropagateExceptionsToEnclosingStatementBlock                  : True
    QuestionMarkVariableValue                                     : True
    LanguageMode                                                  : FullLanguage
    EngineIntrinsics                                              : System.Management.Automation.EngineIntrinsics
    ShellFunctionErrorOutputPipe                                  : System.Management.Automation.Internal.Pipe
    TypeTable                                                     : System.Management.Automation.Runspaces.TypeTable
    ExpressionWarningOutputPipe                                   :
    ExpressionVerboseOutputPipe                                   :
    ExpressionDebugOutputPipe                                     :
    ExpressionInformationOutputPipe                               :
    Events                                                        : System.Management.Automation.PSLocalEventManager
    Debugger                                                      : System.Management.Automation.ScriptDebugger
    PSDebugTraceLevel                                             : 0
    PSDebugTraceStep                                              : False
    ShouldTraceStatement                                          : False
    ScriptCommandProcessorShouldRethrowExit                       : False
    IgnoreScriptDebug                                             : False
    Engine                                                        : System.Management.Automation.AutomationEngine
    RunspaceConfiguration                                         :
    InitialSessionState                                           : System.Management.Automation.Runspaces.InitialSessionSt
                                                                    ate
    IsSingleShell                                                 : True
    PreviousModuleProcessed                                       :
    ModuleBeingProcessed                                          :
    AppDomainForModuleAnalysis                                    :
    AuthorizationManager                                          : Microsoft.PowerShell.PSAuthorizationManager
    ProviderNames                                                 : System.Management.Automation.SingleShellProviderNames
    Modules                                                       : System.Management.Automation.ModuleIntrinsics
    ShellID                                                       : Microsoft.PowerShell
    EngineSessionState                                            : System.Management.Automation.SessionStateInternal
    TopLevelSessionState                                          : System.Management.Automation.SessionStateInternal
    SessionState                                                  : System.Management.Automation.SessionState
    HasRunspaceEverUsedConstrainedLanguageMode                    : False
    UseFullLanguageModeInDebugger                                 : False
    IsModuleWithJobSourceAdapterLoaded                            : False
    LocationGlobber                                               : System.Management.Automation.LocationGlobber
    EngineState                                                   : Available
    HelpSystem                                                    : System.Management.Automation.HelpSystem
    FormatInfo                                                    :
    CustomArgumentCompleters                                      :
    NativeArgumentCompleters                                      :
    CurrentCommandProcessor                                       : format-default
    CommandDiscovery                                              : System.Management.Automation.CommandDiscovery
    EngineHostInterface                                           : System.Management.Automation.Internal.Host.InternalHost
    InternalHost                                                  : System.Management.Automation.Internal.Host.InternalHost
    LogContextCache                                               : System.Management.Automation.LogContextCache
    ExternalSuccessOutput                                         : System.Management.Automation.Internal.ObjectWriter
    ExternalErrorOutput                                           : System.Management.Automation.Internal.ObjectWriter
    ExternalProgressOutput                                        :
    CurrentRunspace                                               : System.Management.Automation.Runspaces.LocalRunspace
    CurrentPipelineStopping                                       : False
    DebugPreferenceVariable                                       : SilentlyContinue
    VerbosePreferenceVariable                                     : SilentlyContinue
    ErrorActionPreferenceVariable                                 : Continue
    WarningActionPreferenceVariable                               : Continue
    InformationActionPreferenceVariable                           : SilentlyContinue
    WhatIfPreferenceVariable                                      : False
    ConfirmPreferenceVariable                                     : High
    FormatDBManager                                               : Microsoft.PowerShell.Commands.Internal.Format.TypeInfoD
                                                                    ataBaseManager
    TransactionManager                                            : System.Management.Automation.Internal.PSTransactionMana
                                                                    ger
    _debugger                                                     : System.Management.Automation.ScriptDebugger
    _debuggingMode                                                : 0
    eventManager                                                  : System.Management.Automation.PSLocalEventManager
    debugTraceLevel                                               : 0
    debugTraceStep                                                : False
    _scriptCommandProcessorShouldRethrowExit                      : False
    _engine                                                       : System.Management.Automation.AutomationEngine
    _runspaceConfiguration                                        :
    _initialSessionState                                          : System.Management.Automation.Runspaces.InitialSessionSt
                                                                    ate
    _previousModuleProcessed                                      :
    _moduleBeingProcessed                                         :
    _responsibilityForModuleAnalysisAppDomainOwned                : False
    _authorizationManager                                         : Microsoft.PowerShell.PSAuthorizationManager
    _modules                                                      : System.Management.Automation.ModuleIntrinsics
    _shellId                                                      : Microsoft.PowerShell
    _languageMode                                                 : FullLanguage
    <HasRunspaceEverUsedConstrainedLanguageMode>k__BackingField   : False
    <IsModuleWithJobSourceAdapterLoaded>k__BackingField           : False
    _locationGlobber                                              : System.Management.Automation.LocationGlobber
    _engineState                                                  : Available
    _helpSystem                                                   : System.Management.Automation.HelpSystem
    _formatInfo                                                   :
    commandFactory                                                : System.Management.Automation.CommandFactory
    myHostInterface                                               : System.Management.Automation.Internal.Host.InternalHost
    _engineIntrinsics                                             : System.Management.Automation.EngineIntrinsics
    _externalErrorOutput                                          : System.Management.Automation.Internal.ObjectWriter
    _externalProgressOutput                                       :
    <PropagateExceptionsToEnclosingStatementBlock>k__BackingField : True
    <CurrentExceptionBeingHandled>k__BackingField                 :
    _questionMarkVariableValue                                    : True
    _typeTable                                                    : System.Management.Automation.Runspaces.TypeTable
    _assemblyCacheInitialized                                     : False
    _moduleNestingDepth                                           : 0

$moduleOutput = $null
$sessionState = [System.Management.Automation.SessionState]::new()
$ExecutionContext._context.Modules.CreateModule('MyModule', 'FakeModulePath', {'FakeModuleOutput'}, $sessionState, [ref]$moduleOutput, @())

    ModuleType Version    Name                                ExportedCommands
    ---------- -------    ----                                ----------------
    Script     0.0        MyModule

$moduleOutput

    FakeModuleOutput
```

Enables implied reflection and explores the current ExecutionContext.  Then creates a module using a non-public method that allows for more control.

## PARAMETERS

### -Force

If specified, this function will not prompt for confirmation before enabling implied reflection.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

## INPUTS

### None

This function does not accept input from the pipeline.

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

This function will not work in scripts as it relys on objects being passed to Out-Default. This is also working as intended, as it's meant to be a tool for exploration and should not be used in scripts.

## RELATED LINKS

[Add-PrivateMember](Add-PrivateMember.md)
[Disable-ImpliedReflection](Disable-ImpliedReflection.md)
