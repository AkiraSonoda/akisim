#!/bin/sh
sleep 60
cd /opt/akisim/bin
# next option may improve SGen gc (for opensim only) you may also need to increase nursery size on large regions
#export MONO_GC_PARAMS="minor=split,promotion-age=14"
mono-sgen ./OpenSim.exe
