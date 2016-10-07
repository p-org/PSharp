event RWant_Machine_ValueReq : machine;
event RWant_Machine_ValueResp : bool;
event RWant_Machine_SetReq : (machine, bool);
event RWant_Machine_SetResp;

machine RWant_Machine
{
	var r_want : bool;

	start state Init
	{
		entry
		{
			r_want = false;
		}

		on RWant_Machine_SetReq do  (payload : (machine, bool))
		{
            r_want = payload.1;
            send payload.0, RWant_Machine_SetResp;
		}

		on RWant_Machine_ValueReq do (target : machine)
		{
            send target, RWant_Machine_ValueResp, r_want;
		}
	}
}
