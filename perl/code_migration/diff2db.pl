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

if ($num_args != 3 ) {
 	usage();
 	exit;
}

my $filepath = $ARGV[0];
my $repo_short_name = $ARGV[1];
my $branch_name = $ARGV[2];

my $dbh = DBI->connect('DBI:mysql:code_migration', 'opensim', 'akira'
	           ) || die "Could not connect to database: $DBI::errstr";
# open diff file
open FILE, $filepath or die $!;

my $original_base;
my $modified_base;
my $orig_file;
my $mod_file;
my $id_repo_branch;

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
		
		$id_repo_branch = &getIdOfRepo();
		my $existsInDB = &existsInDb( $orig_file_path );
		
		if ( $existsInDB == 0 ) {
			print "File does not exist in database\n";
			insertInSourceFiles($orig_file_path, $mod_file_path);
		} else {
			print "File existis in database\n";
			insertInRepoSource($id_repo_branch, $existsInDB);
		};
	}
}

# end of main disconnect from the database
$dbh->disconnect(); 


# check the existence of a given FilePath in the database
sub existsInDb {
	my $filename = shift @_;
	
	my $sth = $dbh->prepare('SELECT * FROM source_files WHERE os_file_name = ?')
              or die "Couldn't prepare statement: " . $dbh->errstr;
	
	$sth->execute($filename)
    or die "Couldn't execute statement: " . $sth->errstr;
	
	if ($sth->rows == 0) {
    	$sth->finish;
    	return 0;
    } else {
    	my $result = $sth->fetchrow_hashref();
    	$sth->finish;
    	return $result->{index};
    }

}

# returns the if number of the given repository short name and the given branch name
sub getIdOfRepo {
	my $sth = $dbh->prepare('SELECT * FROM code_migration.repo_branch WHERE repo_short_name=? AND branch_name=?');
	$sth->execute($repo_short_name, $branch_name);
	my $result = $sth->fetchrow_hashref();
	$sth->finish;
	return $result->{idrepo_branch};	
}

# inserts the file names from the scanned diff file into the source_files table and the corresponding repo_source connection table
sub insertInSourceFiles {
	my $orig_file = shift @_;
	my $mod_file = shift @_;
	
	$dbh->do('INSERT INTO source_files (os_file_name, akisim_file_name) Values (?,?)', undef, $orig_file, $mod_file)
       	or die "Couldn't do statement: " . $dbh->errstr;
    
    # Now we have to get the resulting index which was generated during the insert
   	my $sth = $dbh->prepare('SELECT * FROM source_files WHERE os_file_name = ?')
       or die "Couldn't prepare statement: " . $dbh->errstr;
	$sth->execute($orig_file);
    my $result = $sth->fetchrow_hashref();
    
    # Now we have to insert the index into the repo_source Table
	$dbh->do('INSERT INTO repo_source (idrepo, idsource) Values (?,?)', undef, $id_repo_branch, $result->{index})
       	or die "Couldn't do statement: " . $dbh->errstr;
    
}

# inserts the ids from the source_files table and the corresponding repo_branch connection table repo_source, if the connection does not exist already.
sub insertInRepoSource {
	my $id_repo = shift @_;
	my $id_source = shift @_;
	
	# check if the connection already exists
	my $sth = $dbh->prepare('SELECT * FROM code_migration.repo_source WHERE idrepo = ? AND idsource =?')
                    or die "Couldn't prepare statement: " . $dbh->errstr;
	
	$sth->execute($id_repo, $id_source)
          or die "Couldn't execute statement: " . $sth->errstr;

	# if no rows are found the connection does not exist and therefore has to be crated
	if ($sth->rows == 0) {
		$dbh->do('INSERT INTO repo_source (idrepo, idsource) Values (?,?)', undef, $id_repo, $id_source)
       		or die "Couldn't do statement: " . $dbh->errstr;		
	}

}


# Prints usage
sub usage {
	print "\n";
	print "Usage: diff2db [diff filepath] [repo short name] [branch name]\n\n";
	print "checks a given diff file of the comparison of two file trees \n";
	print "e.g. compare the opensim tree with the akisim tree\n";
	print "and writes the compared file names into the source_code file of\n";
	print "the code_migration database and connects them with the given \n";
	print "repository and branch name\n";
}	
