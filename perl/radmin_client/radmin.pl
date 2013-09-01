#!/usr/bin/perl

BEGIN {
    # because the opensim http server just accept old HTTP/1.0 protocol!!!
    $ENV{PERL_LWP_USE_HTTP_10} ||= 1;  # default to http/1.0
}

$num_args = $#ARGV + 1;
if ($num_args != 1) {
  print "\nUsage: radmin.pl command\n";
  exit;
}


use RPC::XML::Client;

my $client = new RPC::XML::Client('http://localhost:9700');
my $req;
my $res;

$req = RPC::XML::request->new(
            'admin_console_command',
            {
                password => 'akira',
                command => $ARGV[0]
            }
);

$res = $client->send_request($req);
print $res;

