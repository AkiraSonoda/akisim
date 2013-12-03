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
# diff2db 
# 
# reads a diff file with the comparison two file Trees and compares the files
# contained in the diff with the records in the source_code table of the 
# code_migration database
# 
# The File has the following format:
#
# === Left:  /Users/markusgasser/develop/opensim/OpenSim/
# === Right: /Users/markusgasser/develop/akisim/OpenSim/
# === Summary: Files(1470+4eq+0qm 65neq 16np) Folders(543p 8np)
# === Showing Equal: no
# === Showing Equivalent: no
# === Showing QuickMatch: no
# === Showing Peerless: no
# === Showing Folders: no
# === Showing Errors: yes
# === ================================================================
# Files 'Addons/Groups/ForeignImporter.cs' and 'Addons/Groups/ForeignImporter.cs' are different
# Files 'Addons/Groups/GroupsMessagingModule.cs' and 'Addons/Groups/GroupsMessagingModule.cs' are different
# Files 'Addons/Groups/GroupsModule.cs' and 'Addons/Groups/GroupsModule.cs' are different
# Files 'Addons/Groups/Hypergrid/GroupsServiceHGConnectorModule.cs' and 'Addons/Groups/Hypergrid/GroupsServiceHGConnectorModule.cs' are different
# Files 'ApplicationPlugins/LoadRegions/LoadRegionsPlugin.cs' and 'ApplicationPlugins/LoadRegions/LoadRegionsPlugin.cs' are different

use strict;
use constant { true => 1, false => 0 };
use DBI;

my $num_args = $#ARGV + 1;

if ($num_args != 1 ) {
 	usage();
 	exit;
}

my $filepath = $ARGV[0];

my $dbh = DBI->connect('DBI:mysql:code_migration', 'opensim', 'akira'
	           ) || die "Could not connect to database: $DBI::errstr";
# open diff file
open FILE, $filepath or die $!;

my $original_base;
my $modified_base;
my $orig_file;
my $mod_file;

# check every line
while (<FILE>) { 
	if ($_ =~ m/=== Left:\s*\/.*\/(?P<simdir>.*)\/OpenSim\/$/) {
		$original_base = $1;
	}
	if ($_ =~ m/=== Right:\s*\/.*\/(?P<simdir>.*)\/OpenSim\/$/) {
		$modified_base = $1;
	}
	if ($_ =~ m/Files \'(?P<orig_file>.*)\' and \'(?P<mod_file>.*)\' are different/) {
		$orig_file = $1;
		$mod_file = $2;	
					
		my $orig_file_path = $original_base . "/OpenSim/" . $orig_file;
		my $mod_file_path = $modified_base . "/OpenSim/" . $mod_file;
		
		if ( !existsInDb( $orig_file_path ) ) {
			print "File does not exist in database\n";
			insertInDb($orig_file_path, $mod_file_path);
		} else {
			print "File existis in database\n";
		};
	}
}

# end of main disconnect from the database
$dbh->disconnect(); 


# check the existence of a given FilePath in the database
sub existsInDb {
	my $filename = shift @_;
	
	my $sth = $dbh->prepare('SELECT * FROM code_migration.source_files WHERE os_file_name = ?')
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

sub insertInDb {
	my $orig_file = shift @_;
	my $mod_file = shift @_;
	
	$dbh->do('INSERT INTO source_files (os_file_name, akisim_file_name) Values (?,?)', undef, $orig_file, $mod_file)
       	or die "Couldn't do statement: " . $dbh->errstr;	
}

# Prints usage
sub usage {
	print "\n";
	print "Usage: diff2db [diff filepath]\n\n";
	print "checks a given diff file of the comparison of two file trees \n";
	print "e.g. compare the opensim tree with the akisim tree\n";
	print "and writes the compared file names into the source_code file of\n";
	print "the code_migration database\n";
}	
