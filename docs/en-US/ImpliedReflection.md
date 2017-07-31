---
Module Name: ImpliedReflection
Module Guid: 8834a5bf-9bf2-4f09-8415-3c1e561109f6 8834a5bf-9bf2-4f09-8415-3c1e561109f6
Download Help Link:
Help Version: 1.0.0
Locale: en-US
---

# ImpliedReflection Module

## Description

Explore private properties and methods as if they were public.

## ImpliedReflection Cmdlets

### [Add-PrivateMember](Add-PrivateMember.md)

The Add-PrivateMember function binds all non-public members to an object in the same way the PowerShell engine binds public members. This allows the members to be viewed and invoked like any other property or method typically bound by PowerShell.  Properties will be added as PSProperty objects and Methods will be bound as PSMethod objects.

### [Disable-ImpliedReflection](Disable-ImpliedReflection.md)

The Disable-ImpliedReflection function will disable the binding of non-public members to any object outputted to the console.

### [Enable-ImpliedReflection](Enable-ImpliedReflection.md)

The Enable-ImpliedReflection function replaces the Out-Default cmdlet with a proxy function that invokes Add-PrivateMember on every object outputted to the console.
