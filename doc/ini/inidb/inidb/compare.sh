#!/bin/bash 

DevelHome="/home/opensim/src"

# checking Metro inis
# echo "==== check (latest) Metro AkiSim OpenSim.ini against Database ========================"
# perl checkAkiOpenSimIniAgainstDatabase.pl "$DevelHome/akisim/doc/metropolis/ini/OpenSim.ini" Metropolis
# echo "==== check (latest) Metro Distribution OpenSim.ini against Database ========================"
# perl checkOpenSimIniAgainstDatabase.pl "$DevelHome/metrosim/bin/OpenSim.ini" Metropolis
# echo "==== check (latest) Metro Distribution OpenSimDefaults.ini against Database ========================"
# perl checkDefaultIniAgainstDatabase.pl "$DevelHome/metrosim/bin/OpenSimDefaults.ini"
# echo "==== check Database ageinst (latest) Metro AkiSim OpenSim.ini ========================"
# perl checkDatabaseAgainstAkiOpenSimIni.pl "$DevelHome/akisim/doc/metropolis/ini/OpenSim.ini" Metropolis
# echo "==== check Database ageinst (latest) Metro Distribution OpenSim.ini ========================"
# perl checkDatabaseAgainstOpenSimIni.pl "$DevelHome/metrosim/bin/OpenSim.ini" Metropolis
# echo "==== check Database ageinst (latest) Metro Distribution OpenSimDefaults.ini ========================"
# perl checkDatabaseAgainstDefaultIni.pl "$DevelHome/metrosim/bin/OpenSimDefaults.ini" 

# echo ""
# echo ""
# echo ""

# checking OSgrid inis
echo "==== check (lstest) OSgrid AkiSim OpenSim.ini against Database ========================"
perl checkAkiOpenSimIniAgainstDatabase.pl "$DevelHome/akisim/doc/osgrid/ini/OpenSim.ini" OSgrid
echo "==== check (latest) OSgrid Distribution OpenSim.ini against Database ========================"
perl checkOpenSimIniAgainstDatabase.pl "$DevelHome/osgridsim/bin/OpenSim.ini" OSgrid
echo "==== check (latest) OSgrid Distribution OpenSimDefaults.ini against Database ========================"
perl checkDefaultIniAgainstDatabase.pl "$DevelHome/osgridsim/bin/OpenSimDefaults.ini"
echo "==== check Database ageinst (latest) OSgrid AkiSim OpenSim.ini ========================"
perl checkDatabaseAgainstAkiOpenSimIni.pl "$DevelHome/akisim/doc/osgrid/ini/OpenSim.ini" OSgrid
echo "==== check Database ageinst (latest) OSgrid Distribution OpenSim.ini ========================"
perl checkDatabaseAgainstOpenSimIni.pl "$DevelHome/osgridsim/bin/OpenSim.ini" OSgrid
echo "==== check Database ageinst (latest) OSgrid Distribution OpenSimDefaults.ini ========================"
perl checkDatabaseAgainstDefaultIni.pl "$DevelHome/osgridsim/bin/OpenSimDefaults.ini" 

echo ""
echo ""
echo ""

echo "==== check OpenSim (GIT) OpenSimDefaults.ini against Database ========================"
perl checkDefaultIniAgainstDatabase.pl "$DevelHome/opensim/bin/OpenSimDefaults.ini"
echo "==== check Database ageinst (latest) OSgrid Distribution OpenSimDefaults.ini ========================"
perl checkDatabaseAgainstDefaultIni.pl "$DevelHome/opensim/bin/OpenSimDefaults.ini" 
