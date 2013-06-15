#!/usr/bin/perl
#
# THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
# EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
# WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
# DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
# DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
# (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
# LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
# ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
# (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
# SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#
# Lade das aktuellste oar file in den sim 
#
# Prerequisite: der sim enthält genau eine Region
#
# @param directory name. Der Verzeichnisname des Sims
#
# (c) Akira Sonoda, 2012
# Version 1.0
#

use strict;
use File::Find;
use POSIX;
use constant DEBUG => 0;

my $numArgs = $#ARGV + 1;

if( $numArgs != 1 ) {
   die("Usage: perl loadoar.pl [sim directory]");
}

my $sim_directory = $ARGV[0];
if(DEBUG) {
  print "SimDirectory: $sim_directory\n";
}


# Get the name of the oar file
my $oar_file_name = &getOarFileName;
if(DEBUG) {
  print "Result of getOarFileName: $oar_file_name\n";
}

# find the latest oar file
my $biggest_date = 0;
my $file_path = "";

find(\&Wanted,"/home/opensim/backup/$sim_directory");

if(DEBUG) {
	print "Latest $oar_file_name: $file_path\n";
}

# check if the sim process is running
my $running = `ps ax | grep "$sim_directory" | grep -v grep | grep -v loadoar.pl`;
if(DEBUG) {
	print "Process Info: $running";
}
if($running) {
	my $command = "\"load oar --skip-assets $file_path\"";
	`/usr/bin/screen -S $sim_directory -p 0 -X stuff $command`;
	`/usr/bin/screen -S $sim_directory -p 0 -X eval \"stuff ^M\"`;
	print "Upload of oar: $file_path startet.\n";
	print "Depending on the size it will use several minutes to complete.\n";
	print "Laden der oar: $file_path gestartet.\n";
	print "Abhängig von der Grösse des Archivs dauert das einige Minuten bis alles erledigt ist.\n";
} else {
	print "Sim Process with name: $sim_directory is not running";
}


sub getOarFileName {
  if(DEBUG) {
    print "getOarFileName called: \n";
  }
  
  my $find = "save\\s+oar.+\/home\/opensim\/backup\/$sim_directory\/(.*\.oar)";

  open FILE, "</home/opensim/bin/$sim_directory.backup";
  my @lines = <FILE>;
  for (@lines) {
    if ($_ =~ /$find/) {
		chomp($1);
		return $1;
    }
  } 
}

sub Wanted {
	if($_ =~ /$oar_file_name/) {
		my $date = POSIX::strftime("%y%m%d",localtime((stat $File::Find::name)[9]));
		if(DEBUG) {
			print "FileName: $File::Find::name --> $date\n";
		}
		if($date > $biggest_date) {
			$biggest_date = $date;
			$file_path = $File::Find::name
		}
	}
}

exit(0);

