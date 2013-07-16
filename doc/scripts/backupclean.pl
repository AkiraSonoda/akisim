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
# Räume die Backups auf
#
# Prerequisite: installiere libdate-calc-perl ( auf Debian Systemen )
#                           perl-Date-Calc ( auf openSuse Systemen )
#
# @param days. Wieviele Tage Backup sollen erhalten bleiben
#
# (c) Akira Sonoda, 2013
# Version 1.0
#

use strict;
use File::Find;
use File::Path qw(remove_tree);
use Date::Calc qw(:all);
use Scalar::Util qw(looks_like_number);

use constant DEBUG => 1;
use constant GMTTIME => 1;
use constant LOCALTIME => 0;

my $numArgs = $#ARGV + 1;

if( $numArgs != 1 ) {
   die("Usage: perl backupclean.pl [days]");
}

my $days = $ARGV[0];
if(!looks_like_number($days)) {
   die("Usage: perl backupclean.pl [days] \n[days] is expected to be numeric\nCurrent Value: $days\n");

}

if(DEBUG) {
  print "Number of Days to go back: $days\n";
}
# my ($sec,$min,$hour,$mday,$mon,$year,$wday,$yday,$isdst) = Localtime([time]);
my ($year,$month,$day, $hour,$min,$sec, $doy,$dow,$dst) = System_Clock([LOCALTIME]);

if(DEBUG) {
  print "Now is: \n";
  print "Sec   : $sec\n";
  print "Min   : $min\n";
  print "Hour  : $hour\n";
  print "Day   : $day\n";
  print "Month : $month\n";
  print "Year  : $year\n";
  print "WDay  : $dow\n";
  print "YDay  : $doy\n";
  print "isDst : $dst\n";
}

my ($ryear, $rmonth, $rday) = Add_Delta_Days($year,$month,$day,-$days);
if(DEBUG) {
  print "Reference Date - year: $ryear, month: $rmonth, day: $rday \n";
}

find(\&Wanted,"/home/opensim/backup");

sub Wanted {
  my $searchString = "^(\\d{4})(\\d{2})(\\d{2})\\d{6}";
  if($_ =~ /^(\d{4})(\d{2})(\d{2})\d{6}/) {
    my $fyear = $1;
    my $fmonth = $2;
    my $fday = $3;
    
    if(Delta_Days($fyear,$fmonth,$fday,$year,$month,$day) > $days ) {
      if($fday ne "01") {
	print("File $File::Find::name will be deleted\n" );
	remove_tree($File::Find::name); 
      } else {
	print("File $File::Find::name will be kept\n" );
      }
    } else {
      print("File $File::Find::name will be kept\n" );
    }
  }
}

exit(0);