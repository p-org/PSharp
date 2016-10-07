#include "Client.p"
#include "Server.p"

machine Main
{
	var client : machine;
	var server : machine;
	var lk_machine : machine;
	var rlock_machine : machine;
	var rwant_machine : machine;
	var state_machine : machine;

	start state Init
	{
		entry
		{
			lk_machine = new Lk_Machine();
            rlock_machine = new RLock_Machine();
            rwant_machine = new RWant_Machine();
            state_machine = new State_Machine();
            client = new Client(lk_machine, rlock_machine, rwant_machine, state_machine);
            server = new Server(lk_machine, rlock_machine, rwant_machine, state_machine);
		}
	}
}
