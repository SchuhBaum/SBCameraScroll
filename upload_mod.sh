#!/bin/bash

# This uses my rain_world_uploader for Linux.
# Link: https://github.com/SchuhBaum/rain_world_uploader/

mod_id="2928752589"
mod_name="SBCameraScroll"

prev_wd="$(pwd)"

cur_wd_relative="$(dirname "${BASH_SOURCE[0]}")"
cur_wd="$(cd $cur_wd_relative && pwd)"

$HOME/rain_world_uploader/rain_world_uploader.out "$mod_id" "./$mod_name"

cd "$prev_wd"
