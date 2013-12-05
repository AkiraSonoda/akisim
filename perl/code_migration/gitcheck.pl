# 
# Copyright (C) 2013 Akira Sonoda
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
#
# Only those files marked to be under the GNU General Public License are
# under this license other parts of the source archive are under other
# licenses. Please check those files seperately.
#
# gitcheck
# 
# reads a diff file which was created using the git diff command. The script 
# checks if one of the files exists in the source_files table of the code_migration database.
# if this is the case a warning will be issued to the console. That means. Something was changed 
# in a file which was modified in another repository and the checkin needs closer inspection.
# 
# The diff File has the following format:
#
# ------------------------------ 8< ---------------------------
# diff --git a/OpenSim/Region/Framework/Scenes/ScenePresence.cs b/OpenSim/Region/Framework/Scenes/ScenePresence.cs
# index dae20a5..1dc7e20 100644
# --- a/OpenSim/Region/Framework/Scenes/ScenePresence.cs
# +++ b/OpenSim/Region/Framework/Scenes/ScenePresence.cs
# @@ -1648,24 +1648,12 @@ namespace OpenSim.Region.Framework.Scenes
#  
#              if (AllowMovement && !SitGround)
#              {
# -//                m_log.DebugFormat("[SCENE PRESENCE]: Initial body rotation {0} for {1}", agentData.BodyRotation, Name);
# -
# +                Quaternion bodyRotation = agentData.BodyRotation;
#                  bool update_rotation = false;
#  
# -                if (agentData.BodyRotation.Z != Rotation.Z || agentData.BodyRotation.W != Rotation.W)
# +                if (bodyRotation != Rotation)
#                 {
# -                    Rotation = new Quaternion(0, 0, agentData.BodyRotation.Z, agentData.BodyRotation.W);
# +                    Rotation = bodyRotation;
#                      update_rotation = true;
#                 }
# ------------------------------ 8< ---------------------------
# The interesting part is the line that starts with "---"
use strict;
use constant { true => 1, false => 0 };
use DBI;

my $num_args = $#ARGV + 1;

if ($num_args != 2 ) {
 	usage();
 	exit;
}

my $filepath = $ARGV[0];
my $view_name = $ARGV[1];

my $dbh = DBI->connect('DBI:mysql:code_migration', 'opensim', 'akira'
	           ) || die "Could not connect to database: $DBI::errstr";

# open diff file
open FILE, $filepath or die $!;

my $filename;
my $indexname;

# check every line
while (<FILE>) {
	# get the Index
	if ($_ =~ m/index\s(?P<index>.*)\s\d{6}$/) {
		$indexname = $1;
	}
	
	# get the FileName
	if ($_ =~ m/---\sa(?P<filepath>.*)$/) {
		$filename = $1;
		# check the file name against the database 
		if(existsInDb("opensim".$filename)) {
			print "$indexname modified $filename\n";
		}
	}
	

}

# end of main disconnect from the database
$dbh->disconnect(); 

# check the existence of a given FilePath in the database
sub existsInDb {
	my $filename = shift @_;
	my $sqlstring = "SELECT * FROM $view_name WHERE os_file_name = ?";
	
	my $sth = $dbh->prepare($sqlstring)
              or die "Couldn't prepare statement: " . $dbh->errstr;
	
	$sth->execute($filename)
    or die "Couldn't execute statement: " . $sth->errstr;
	
	if ($sth->rows == 0) {
    	$sth->finish;
    	return false;
    } else {
    	$sth->finish;
    	return true;
    }

}



# Prints usage
sub usage {
	print "\n";
	print "Usage: gitcheck [filepath] [view to use] \n\n";
	print "checks a given git diff file against the table source_code of the database code_migration \n";
	print "if a filename from the contained in the diff file is found in the database, a warning is\n";
	print "issued to the console stating a modification is contained in the diff which affects a file modified in another repository\n";
	print "\n";
	print "[filepath] the path to the diff file produced with the git command";
	print "[view to use] the view name to be used, because each repo/branch has its own view which could contain different file names";
}	

