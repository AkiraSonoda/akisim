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
# checkDefaultIniAgainstDatabase
# 
# checks a given OpenSim ini File e.g. OpenSim.ini, OpenSimDefaults.ini
# against the opensim_ini database and reports differences to the console.
# 

use strict;
use Config::IniFiles;

use constant { true => 1, false => 0 };

use DBI;

if ($#ARGV != 0 ) {
	print "\n";
	print "Usage: checkDefaultIniAgainstDatabase INIFILE\n\n";
	print "Checks a given INIFILE of type OpenSimDefault.ini against the ini database and reports differences to the console\n";
	print "Valid files are: \n\n";
	print "  - OpenSimDefaults.ini\n";
	print "  - OpenSim.ini\n";
	exit;
}

my $filepath = $ARGV[0];

my $dbh = DBI->connect('DBI:mysql:opensim_ini', 'opensim', 'opensim'
	           ) || die "Could not connect to database: $DBI::errstr";

# Print the Header Line
print "ini_section;ini_parameter;opensim_value;ini_value;Comment\n";				

my $cfg = Config::IniFiles->new( -file => $filepath );

my @sections=$cfg->Sections();

foreach my $section (@sections) {
    #Retreives all parameters in the section
    my @params = $cfg->Parameters($section);
    #For each parameter in a particular compare the value with the value stored in the database
    findInDatabase ($section,@params);
}

sub findInDatabase {
    my $section = shift @_;
	my @parameters = @_;
	my $sth;
    foreach my $parameter (@parameters) {
        chomp($parameter);
        chomp($section);
		#This below statement is not printing out the values.
		my $ini_value = $cfg->val($section, $parameter);
		if ($ini_value =~ m/(.*)\;/) {
			$ini_value = trim($1);
		} else {
			$ini_value = trim($ini_value);
		}
		
		my $sql = qq'SELECT opensim_value, opensim_enabled_default FROM ini WHERE ini_section="$section" AND ini_parameter="$parameter"';
		$sth = $dbh->prepare($sql)or die "Cannot prepare: " . $dbh->errstr();
		$sth->execute() or die "Cannot execute: " . $sth->errstr();
		my($opensim_value,$opensim_enabled_default) = $sth->fetchrow_array();
		if($opensim_value ne undef) {
	  		if($opensim_value ne $ini_value) {
  				print "$section;$parameter;$opensim_value;$ini_value;different_values\n";		
  			} elsif ($opensim_enabled_default == false ) {
  				print "$section;$parameter;$opensim_value;$ini_value;disabled_in_database\n";		  				
  			}
		} else {
  			print "$section;$parameter;--;$ini_value;not found in database\n";		  				
		}
    }
}

# Perl trim function to remove whitespace from the start and end of the string
sub trim($) {
	my $string = shift;
	$string =~ s/^\s+//;
	$string =~ s/\s+$//;
	return $string;
}

	









$dbh->disconnect();