
. ".\variables.ps1";

#

Write-Host "Running build.ps1..."
& "$dir\windows_build.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting."
    exit 1
}

#
#

Push-Location $dir

$item_paths = @(
    "$mod_name\previously_active_mods.json",
    "$mod_name\plugins\profiler.dll",
    "$mod_name\plugins\profiler.pdb"
)

foreach ($item_path in $item_paths) {
    if (Test-Path $item_path) {
        Remove-Item -Path "$item_path" -Force
    }
}

7z a $zip_path "$mod_name\AssetBundles\modded_shaders" "$mod_name\plugins\$dll_name" "$mod_name\plugins\$pdb_name" "$mod_name\modinfo.json" "$mod_name\thumbnail.png" "$mod_name\workshopdata.json"

Pop-Location

Write-Host "Archive created at: $zip_path"

