#
# Module manifest for module 'ImpliedReflection'
#
# Generated by: Patrick Meinecke
#
# Generated on: 7/23/2017
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'ImpliedReflection.psm1'

# Version number of this module.
ModuleVersion = '0.1.1'

# ID used to uniquely identify this module
GUID = '8834a5bf-9bf2-4f09-8415-3c1e561109f6'

# Author of this module
Author = 'Patrick Meinecke'

# Company or vendor of this module
CompanyName = 'Community'

# Copyright statement for this module
Copyright = '(c) 2017 Patrick Meinecke. All rights reserved.'

# Description of the functionality provided by this module
Description = 'Explore private properties and methods as if they were public.'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '5.1'

# Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
DotNetFrameworkVersion = '4.0'

# Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
CLRVersion = '4.0'

# Processor architecture (None, X86, Amd64) required by this module
ProcessorArchitecture = 'None'

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = 'Add-PrivateMember',
                    'Disable-ImpliedReflection',
                    'Enable-ImpliedReflection'

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# List of all files packaged with this module
FileList = 'ImpliedReflection.psd1',
           'ImpliedReflection.psm1',
           'Public\Add-PrivateMember.ps1',
           'Public\Disable-ImpliedReflection.ps1',
           'Public\Enable-ImpliedReflection.ps1'

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = @()

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/SeeminglyScience/ImpliedReflection/blob/master/LICENSE'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/SeeminglyScience/ImpliedReflection'

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        ReleaseNotes = '- Fixed the module not loading.'

    } # End of PSData hashtable

} # End of PrivateData hashtable

}



