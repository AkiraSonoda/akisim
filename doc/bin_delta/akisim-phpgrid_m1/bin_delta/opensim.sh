#!/bin/sh
sleep 60
# next option may improve SGen gc (for opensim only) you may also need to increase nursery size on large regions
#export MONO_GC_PARAMS="minor=split,promotion-age=14"
cd /opt/akisim/bin/
mono-sgen OpenSim.exe
