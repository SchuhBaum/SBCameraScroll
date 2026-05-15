
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

# `Release` sounds nice because there are plenty of performance issues in the
# game and I don't want to be part of that. I don't think this will save me from
# that but might make a difference in some cases.
$config = "Release"
# $config = "Debug"

$mod_name = "SBCameraScroll"
$dll_name = "$mod_name.dll"
$pdb_name = "$mod_name.pdb"

$asset_bundles_path = "$dir\$mod_name\assetbundles"

$downloads_path = "$HOME\downloads"
$zip_path = "$downloads_path\$mod_name.zip"
