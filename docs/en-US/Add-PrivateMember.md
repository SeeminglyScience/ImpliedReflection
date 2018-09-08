---
external help file: ImpliedReflection-help.xml
online version: https://github.com/SeeminglyScience/ImpliedReflection/blob/master/docs/en-US/Add-PrivateMember.md
schema: 2.0.0
---

# Add-PrivateMember

## SYNOPSIS

Bind all non-public members to an object.

## SYNTAX

```powershell
Add-PrivateMember [[-ReturnPropertyName] <String>] [-InputObject <PSObject>] [-PassThru]
```

## DESCRIPTION

The Add-PrivateMember function binds all non-public members to an object in the same way the PowerShell engine binds public members. This allows the members to be viewed and invoked like any other property or method typically bound by PowerShell.  Properties will be added as PSProperty objects and Methods will be bound as PSMethod objects.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
$ExecutionContext | Add-PrivateMember
```

Binds all private members to the EngineIntrinsics object contained in the global variable
$ExecutionContext.

## PARAMETERS

### -ReturnPropertyName

Specifies the property to return after binding members. This can be used to chain Add-PrivateMember calls to quickly traverse an object without outputting it to the console.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject

Specifies the object aquire and bind private members for.

```yaml
Type: PSObject
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru

If specified, objects specified in the InputObject parameter will be returned to the pipeline after member binding.

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

### PSObject

You can pipe any object to this function.

## OUTPUTS

### None, System.Object

If either the ReturnPropertyName parameter or the PassThru switch parameter is specified, any object can be returned.  Otherwise this function does not have output.

## NOTES

- If the InputObject is a type info object (System.Type) then static members of the value will be added instead.
- Non-public constructors will be added as a static method named "ctor".
- Properties or fields with the same name of an existing property will not be added.
- Non-public method overloads of a public method will not be loaded.
- Overloads of a method that is not already present will be grouped into a single PSMethod object,
  like you would see in a method bound by the PowerShell engine.

## RELATED LINKS

[Enable-ImpliedReflection](Enable-ImpliedReflection.md)
[Disable-ImpliedReflection](Disable-ImpliedReflection.md)
