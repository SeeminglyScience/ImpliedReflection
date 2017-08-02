Import-LocalizedData -BindingVariable Strings -FileName Strings -ErrorAction Ignore

# In PowerShell 6 a lot of field names are changed to fit .NET standards. This is why you should
# avoid using reflection :)
$instancePrefix = ''
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $instancePrefix = '_'
}
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '',
                                                   Justification='Module scope variable used in other files.')]
$FIELD_REFERENCE = @{
    adapter             = "${instancePrefix}adapter"
    indexes             = "${instancePrefix}indexes"
    # These haven't been changed yet, but probably will soon.  Keeping it here so it's easy to fix.
    dotNetStaticAdapter = 'dotNetStaticAdapter'
    readOnly            = 'readOnly'
    writeOnly           = 'writeOnly'
}

# Include all function files.
Get-ChildItem $PSScriptRoot\Public\*.ps1 | ForEach-Object {
    . $PSItem.FullName
}

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-*

