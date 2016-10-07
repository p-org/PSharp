 event RLock_Machine_ValueReq : machine;
 event RLock_Machine_ValueResp : bool;
 event RLock_Machine_SetReq : (machine, bool);
 event RLock_Machine_SetResp;

 machine RLock_Machine
 {
	var r_lock : bool;
	
	start state Init 
	{
		entry
		{
			r_lock = false;
		}

		on RLock_Machine_SetReq do (payload : (machine, bool)) 
		{
            r_lock = payload.1;
            send payload.0, RLock_Machine_SetResp;
		}

		on RLock_Machine_ValueReq do (target : machine)
		{
            send target, RLock_Machine_ValueResp, r_lock;
		}
	}
 }