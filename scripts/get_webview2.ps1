$ProgressPreference = 'SilentlyContinue'
$url = 'https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/1.0.2903.40'
$zip = 'wv2.zip'

Write-Host "Downloading WebView2 package..."
(New-Object System.Net.WebClient).DownloadFile($url, $zip)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zip).Path)

if (-not (Test-Path lib)) { New-Item -ItemType Directory lib | Out-Null }
if (-not (Test-Path bin)) { New-Item -ItemType Directory bin | Out-Null }
if (-not (Test-Path bin\runtimes\win-x64\native)) { New-Item -ItemType Directory bin\runtimes\win-x64\native -Force | Out-Null }

Write-Host "Extracting required DLLs..."
foreach ($entry in $archive.Entries) {
    if ($entry.FullName -eq 'lib/net462/Microsoft.Web.WebView2.WinForms.dll') {
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'lib').Path + '\Microsoft.Web.WebView2.WinForms.dll', $true)
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'bin').Path + '\Microsoft.Web.WebView2.WinForms.dll', $true)
    }
    if ($entry.FullName -eq 'lib/net462/Microsoft.Web.WebView2.Core.dll') {
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'lib').Path + '\Microsoft.Web.WebView2.Core.dll', $true)
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'bin').Path + '\Microsoft.Web.WebView2.Core.dll', $true)
    }
    if ($entry.FullName -eq 'runtimes/win-x64/native/WebView2Loader.dll') {
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'bin').Path + '\WebView2Loader.dll', $true)
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'bin\runtimes\win-x64\native').Path + '\WebView2Loader.dll', $true)
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Resolve-Path 'lib').Path + '\WebView2Loader.dll', $true)
    }
}
$archive.Dispose()
Remove-Item $zip -Force
Write-Host "WebView2 libraries ready!"
