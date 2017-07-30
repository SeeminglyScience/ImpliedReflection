$moduleName   = 'ImpliedReflection'
$manifestPath = "$PSScriptRoot\..\module\$moduleName.psd1"

Import-Module $manifestPath
Describe 'Session isn''t dirty ' {
    It 'has a normal $ExecutionContext' {
        $ExecutionContext.psobject.Properties.Item('_context') | Should BeNullOrEmpty
    }
}
Describe 'Add-PrivateMember operation' {
    Context 'instance checks' {
        It 'can add private instance members' {
            Add-PrivateMember -InputObject $ExecutionContext
        }
        It 'can access fields' {
            $ExecutionContext._context | Should Not BeNullOrEmpty
            $ExecutionContext._context.GetType().Name | Should Be 'ExecutionContext'
        }
        It 'can access properties' {
            Add-PrivateMember -InputObject $ExecutionContext.SessionState
            $ExecutionContext.SessionState.Internal.GetType().Name | Should Be 'SessionStateInternal'
        }
        It 'can invoke methods' {
            Add-PrivateMember -InputObject $ExecutionContext.SessionState.Internal
            $ExecutionContext.SessionState.Internal.GetFunction('prompt') |
                Should BeOfType 'System.Management.Automation.FunctionInfo'
        }
        It 'can invoke methods with ByRef parameters' {
            $scope = $null
            $ExecutionContext.SessionState.Internal.GetVariableItem('ExecutionContext', [ref]$scope) |
                Should BeOfType 'psvariable'

            $scope.GetType().Name | Should Be 'SessionStateScope'

        }
        It 'does not implicitly add properties without being enabled' {
            $ExecutionContext._context._formatDBManager | Should Be $null
        }
        It 'can chain with return property parameter' {
            $ExecutionContext |
                Add-PrivateMember _context |
                Add-PrivateMember Modules |
                Add-PrivateMember _moduleTable |
                Should BeOfType 'System.Collections.Generic.Dictionary[string,psmoduleinfo]'
        }
        It 'returns the object with PassThru' {
            $ExecutionContext |
                Add-PrivateMember -PassThru |
                Should BeOfType 'System.Management.Automation.EngineIntrinsics'
        }
    }
    Context 'static checks' {
        It 'can add static members' {
            [scriptblock] | Add-PrivateMember
        }
        It 'can access properties' {
            [scriptblock]::EmptyScriptBlock | Should BeOfType scriptblock
        }
        It 'can access fields' {
            [scriptblock]::_cachedScripts.GetType().Name | Should Be 'ConcurrentDictionary`2'
        }
        It 'can access methods' {
            [scriptblock]::TokenizeWordElements('one two three') | Should Be 'one','two','three'
        }
        It 'added methods are the correct type' {
            [scriptblock]::BindArgumentsForScripblockInvoke |
                Should BeOfType 'System.Management.Automation.PSMethod'
        }
    }
}
