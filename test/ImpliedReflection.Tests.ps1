$moduleName   = 'ImpliedReflection'
$manifestPath = "$PSScriptRoot\..\module\$moduleName.psd1"

Describe 'module manifest values' {
    It 'can retrieve manfiest data' {
        $script:manifest = Test-ModuleManifest $manifestPath -ErrorAction Ignore
        $script:manifest | Should Not BeNullOrEmpty
    }
    It 'has the correct name' {
        $script:manifest.Name | Should Be $moduleName
    }
    It 'has the correct guid' {
        $script:manifest.Guid | Should Be '8834a5bf-9bf2-4f09-8415-3c1e561109f6'
    }
}
