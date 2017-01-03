#include "Server.p"
#include "Client1.p"
#include "Client2.p"

machine Main
{
	start state Init
	{
		entry
		{
			var server : machine;
			server = new Server();
			new Client1(server);
			new Client2(server);
		}
	}
}
