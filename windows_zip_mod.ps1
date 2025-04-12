
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

Write-Host "Running build.ps1..."
& "$dir\windows_build.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting."
    exit 1
}

#
#

$mod_name = "SBCameraScroll"
$dll_name = "$mod_name.dll"
$pdb_name = "$mod_name.pdb"

$downloads_path = "$HOME\downloads"
$zip_path = "$downloads_path\$mod_name.zip"

Push-Location $dir

$item_paths = @(
    "$mod_name\previously_active_mods.json"
)

$folder_paths = @(
    "$mod_name\world",
    "$mod_name\levels"
)

foreach ($item_path in $item_paths) {
    if (Test-Path $item_path) {
        Remove-Item -Path "$item_path" -Force
    }
}

foreach ($folder_path in $folder_paths) {
    if (Test-Path $folder_path) {
        Remove-Item -Path "$folder_path" -Recurse -Force
    }
}

7z a $zip_path "$mod_name\AssetBundles\modded_shaders" "$mod_name\plugins\$dll_name" "$mod_name\plugins\$pdb_name" "$mod_name\modinfo.json" "$mod_name\thumbnail.png" "$mod_name\workshopdata.json"

Pop-Location

Write-Host "Archive created at: $zip_path"

