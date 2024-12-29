#Requires -Version 7

<#
.EXAMPLE
    # populate with example ipas from github:
    populate-with-ipas.ps1

    # populate with local ipas:
    populate-with-ipas.ps1 $(Get-Item C:\foo\bar\*.ipa)
#>



[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [string[]]
    $filesToUpload
)

$Debug = $false

function Push-ToServer {
    param (
        [string] $source
    )

    if ($source -imatch '^https?://') {
        $temp = New-TemporaryFile
        Write-Output "downloading '$source' to '$($temp.FullName)'"
        curl -sLo $temp $source
        Write-Output "uploading '$($temp.FullName)'"
        $verbose = "-s"
        if ($Debug) {
            $verbose = "-v"
        }
        curl $verbose https://localhost:7266/api/packages?token=secret --data-binary `@$temp -H "Content-Type: application/octet-stream"
    } else {
        Write-Output "uploading local file '$source'"
        curl $verbose https://localhost:7266/api/packages?token=secret --data-binary `@$source -H "Content-Type: application/octet-stream"
    }

    Write-Output ''
    Write-Output '------------------------------------------------------------'
}

if ($null -ne $filesToUpload) {
    foreach ($file in $filesToUpload) {
        Push-ToServer $file
    }
    exit
}

# some randomly ipas i found on github, to test the server
$sources = @(
    "https://github.com/microsoft/fastlane-plugin-appcenter/raw/f715499df469aa95c3e19139b371c2b26a9b9632/fastlane/app-release.ipa"
    "https://github.com/alfiecg24/TrollInstallerX/releases/download/1.0.3/TrollInstallerX-20D50.ipa"
    "https://github.com/alfiecg24/TrollInstallerX/releases/download/1.0.3/TrollInstallerX.ipa"
    "https://github.com/alfiecg24/TrollInstallerX/releases/download/1.0.2/TrollInstallerX.ipa"
    "https://github.com/alfiecg24/TrollInstallerX/releases/download/1.0.0/TrollInstallerX.ipa"
    "https://github.com/SideStore/SideStore/releases/download/0.5.10/SideStore.ipa"
    "https://github.com/SideStore/SideStore/releases/download/0.5.9/SideStore.ipa"
    "https://github.com/SideStore/SideStore/releases/download/0.5.8/SideStore.ipa"
    "https://github.com/utmapp/UTM/releases/download/v4.6.4/UTM-Remote.ipa"
    "https://github.com/utmapp/UTM/releases/download/v4.6.4/UTM.ipa"
    "https://github.com/utmapp/UTM/releases/download/v4.6.4/UTM-HV.ipa"
)

foreach ($source in $sources) {
    Push-ToServer $source
}