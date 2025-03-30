#!/bin/bash

# This uses my rain_world_uploader for Linux.
# Link: https://github.com/SchuhBaum/rain_world_uploader/

prev_wd="$(pwd)"
cur_wd_relative="$(dirname "${BASH_SOURCE[0]}")"
cur_wd="$(cd $cur_wd_relative && pwd)"

./build_linux.sh
zip -r SBCameraScroll.zip ./SBCameraScroll/
rm $HOME/downloads/SBCameraScroll.zip
mv ./SBCameraScroll.zip $HOME/downloads/SBCameraScroll.zip

cd "$prev_wd"


read -p "Upload mod? (yes/NO) $ " ready

if ! [ "$ready" == "yes" ]; then
    echo "Exiting."
    exit 0
fi

mod_id="2928752589"
mod_name="SBCameraScroll"

prev_wd="$(pwd)"
cur_wd_relative="$(dirname "${BASH_SOURCE[0]}")"
cur_wd="$(cd $cur_wd_relative && pwd)"

$HOME/rain_world_uploader/rain_world_uploader.out "$mod_id" "./$mod_name"

cd "$prev_wd"
