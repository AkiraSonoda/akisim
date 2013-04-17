#!/bin/bash 

DevelHome="/mnt/hgfs/MacData/develop"

# generating Metro OpenSim.ini
perl generateOpenSimIni.pl "$DevelHome/akisim/doc/metropolis/ini/OpenSim.ini" Metropolis

# generating OSgrid OpenSim.ini
perl generateOpenSimIni.pl "$DevelHome/akisim/doc/osgrid/ini/OpenSim.ini" OSgrid

echo "OpenSim.ini generated for Metropolis and OSgrid"