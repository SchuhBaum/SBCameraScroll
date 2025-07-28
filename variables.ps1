
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

$config = "Release"

$mod_name = "SBCameraScroll"
$dll_name = "$mod_name.dll"
$pdb_name = "$mod_name.pdb"

$asset_bundles_path = "$dir\$mod_name\assetbundles"

$downloads_path = "$HOME\downloads"
$zip_path = "$downloads_path\$mod_name.zip"
