using namespace System.Reflection
using namespace System.Linq
using namespace System.Management.Automation

function Add-PrivateMember {
    <#
    .EXTERNALHELP ImpliedReflection-help.xml
    #>
    [CmdletBinding(PositionalBinding=$false)]
    param(
        [Parameter(Position=0)]
        [string]
        $ReturnPropertyName,

        [Parameter(ValueFromPipeline)]
        [psobject]
        $InputObject,

        [switch]
        $PassThru
    )
    begin {
        $FLAG_MAP = @{
            StaticAll = [BindingFlags]'Static, NonPublic, Public'
            Static    = [BindingFlags]'Static, NonPublic'
            Instance  = [BindingFlags]'Instance, NonPublic'
        }

        function GetCacheEntry([MemberInfo[]] $member) {
            if ($member[0] -is [MethodBase]) {
                return NewMethodCacheEntry $member
            }
            return NewPropertyCacheEntry $member[0]
        }

        function NewPropertyCacheEntry([MemberInfo] $property) {
            $cacheEntry = [ref].Assembly.
                GetType('System.Management.Automation.DotNetAdapter+PropertyCacheEntry').
                GetConstructor(60, $null, @($property.GetType()), 1).
                Invoke($property)

            # The cache entry uses these fields to determine if the properties for the
            # Getter/Setter delegates should create the delegate. They are set to false during
            # construction if the method isn't public.
            $cacheEntry.GetType().GetField('writeOnly', 60).SetValue($cacheEntry, $false)
            $isWritable = $property.SetMethod -or
                         ('Field' -eq $property.MemberType -and
                          -not $property.IsInitOnly)

            if ($isWritable) {
                $cacheEntry.GetType().GetField('readOnly', 60).SetValue($cacheEntry, $false)
            }
            return $cacheEntry
        }

        function NewMethodCacheEntry([MethodBase[]] $methods) {
            # PowerShell tries to flatten the arrays without explicitly constructing them like this.
            $argumentList = [object[]]::new(1)
            $argumentList[0] = $methods

            return [ref].
                Assembly.
                GetType('System.Management.Automation.DotNetAdapter+MethodCacheEntry').
                GetConstructor($FLAG_MAP.Instance, $null, @([MethodBase[]]), 1).
                Invoke($argumentList)
        }

        function NewPSMethod([Lookup[string, MethodInfo]] $methodGroups) {
            foreach ($methodGroup in $methodGroups) {
                if ($InputObject.psobject.Methods[$methodGroup.Key]) { continue }
                $adapterData = NewMethodCacheEntry $methodGroup

                # Mainly cosmetic, just shows or doesn't show in Get-Member if this is set.
                $isSpecial = $methodGroup.Key -match '(set|get|add|remove)_\w+$'

                $psMethod = [PSMethod].InvokeMember(
                    <# name:       #> '',
                    <# invokeAttr: #> [BindingFlags]'CreateInstance, Instance, NonPublic',
                    <# binder:     #> $null,
                    <# target:     #> $null,
                    <# args:       #> @(
                        <# name:        #> $methodGroup.Key,
                        <# adapter:     #> $bindings.Adapter,
                        <# baseObject:  #> $InputObject.psobject.BaseObject,
                        <# adapterData: #> $adapterData,
                        <# isSpecial:   #> $isSpecial,
                        <# isHidden:    #> $isSpecial))

                # The true here is "preValidated", it throws an exception about not being able to add
                # PSMethods if you don't claim to have validated it.
                $psMethod # yield
            }
        }

        function BindInstanceProperties([MemberInfo[]] $properties) {
            foreach ($property in $properties) {

                if ($InputObject.psobject.Properties[$property.Name]) { continue }

                $adapterData = NewPropertyCacheEntry $property

                $psProperty = [PSProperty].InvokeMember(
                    <# name:       #> '',
                    <# invokeAttr: #> [BindingFlags]'CreateInstance, Instance, NonPublic',
                    <# binder:     #> $null,
                    <# target:     #> $null,
                    <# args:       #> @(
                        <# name:        #> $property.Name,
                        <# adapter:     #> $bindings.Adapter,
                        <# baseObject:  #> $InputObject.psobject.BaseObject,
                        <# adapterData: #> $adapterData))

                $InputObject.psobject.Properties.Add($psProperty, $true)
            }
        }

        function GetCacheTable([string] $memberType) {
            return $bindings.Adapter.
                GetType().
                GetMethod("GetStatic${memberType}ReflectionTable", $FLAG_MAP.Static).
                Invoke($null, @($bindings.Type))
        }

        function GetBindingInfo([psobject] $target) {
            if ($target -is [type]) {
                return @{
                    IsStatic = $true
                    Type     = $target.psobject.BaseObject
                    Flags    = $FLAG_MAP.Static
                    Adapter  = [psobject].
                        GetField('dotNetStaticAdapter', $FLAG_MAP.Static).
                        GetValue($null)
                }
            }
            return @{
                IsStatic = $false
                Type     = $target.GetType()
                Flags    = $FLAG_MAP.Instance
                Adapter  = [PSMethod].
                    GetField('adapter', $FLAG_MAP.Instance).
                    GetValue($target.psobject.Methods.Item('GetType'))
            }
        }
    }
    process {
        $bindings   = GetBindingInfo $InputObject
        $members = @{
            Method    = $bindings.Type.GetMethods($bindings.Flags)
            Property  = $bindings.Type.GetProperties($bindings.Flags) +
                        $bindings.Type.GetFields($bindings.Flags)
        }

        # Group methods by name so we can add them to the same cache entry.
        if ($members.Method) {
            $members.Method = [Enumerable]::ToLookup(
                $members.Method,
                [Func[MethodInfo, string]]{ $args[0].Name })
        }

        if ($bindings.IsStatic) {
            # Cache the add method for the loop.
            $add = [ref].
                Assembly.
                GetType('System.Management.Automation.CacheTable').
                GetMethod('Add', $FLAG_MAP.Instance)

            foreach ($memberType in 'Method', 'Property') {
                $table = GetCacheTable $memberType
                foreach ($member in $members.$memberType) {

                    $memberName = $member.Key
                    if (-not $memberName) { $memberName = $member.Name }

                    $alreadyExists = $table.
                        GetType().
                        GetField('indexes', $FLAG_MAP.Instance).
                        GetValue($table).
                        ContainsKey($memberName)

                    if (-not $alreadyExists) {
                        $cacheEntry = GetCacheEntry $member

                        $null = $add.Invoke($table, @($memberName, $cacheEntry))

                    }
                }
            }
        } else {
            $psMethods = NewPSMethod $members.Method
            foreach ($psMethod in $psMethods) {
                $InputObject.psobject.Methods.Add($psMethod, $true)
            }
            BindInstanceProperties $members.Property
        }
        if ($ReturnPropertyName) {
            $InputObject.$ReturnPropertyName
        } elseif ($PassThru.IsPresent) {
            $InputObject # yield
        }
    }
}
