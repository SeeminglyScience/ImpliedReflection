---
external help file: ImpliedReflection-help.xml
online version: https://github.com/SeeminglyScience/ImpliedReflection/blob/master/docs/en-US/Disable-ImpliedReflection.md
schema: 2.0.0
---

# Disable-ImpliedReflection

## SYNOPSIS

Disables automatic binding of non-public members.

## SYNTAX

```powershell
Disable-ImpliedReflection
```

## DESCRIPTION

The Disable-ImpliedReflection function will disable the binding of non-public members to any object outputted to the console.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
Enable-ImpliedReflection -Force
$ExecutionContext
Disable-ImpliedReflection
$ExecutionContext._context
```

Enables implied reflection, uses it to bind private members to $ExecutionContext, disables implied reflection, and returns one of the members bound.

## PARAMETERS

## INPUTS

### None

This function does not accept input from the pipeline.

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

- This will not remove members bound by implied reflection.

## RELATED LINKS

[Add-PrivateMember](Add-PrivateMember.md)
[Disable-ImpliedReflection](Disable-ImpliedReflection.md)
