#include "RLock_Machine.p"
#include "Lk_Machine.p"
#include "RWant_Machine.p"
#include "State_Machine.p"

event Wakeup;

machine Server
{
	var Lk_MachineId : machine;
	var RLock_MachineId : machine;
	var RWant_MachineId : machine;
	var State_MachineId : machine;

	start state Init
	{
		entry (payload : (machine, machine, machine, machine))
		{
            Lk_MachineId = payload.0;
            RLock_MachineId = payload.1;
            RWant_MachineId = payload.2;
            State_MachineId = payload.3;
            raise Wakeup;
		}

		on Wakeup do
		{
			print "Server waking up\n";
			send RLock_MachineId, RLock_Machine_SetReq, this, false;
            receive
			{
				case RLock_Machine_SetResp : 
				{
					send Lk_MachineId, Lk_Machine_Waiting, this, false;
					receive
					{
						case Lk_Machine_WaitResp : 
						{
							send RWant_MachineId, RWant_Machine_ValueReq, this;
							receive 
							{
								case RWant_Machine_ValueResp : (value : bool)
								{
									if (value == true)
									{
										send RWant_MachineId, RWant_Machine_SetReq, this, false;
										receive 
										{
											case RWant_Machine_SetResp : 
											{
												send State_MachineId, State_Machine_ValueReq, this;
												receive 
												{
													case State_Machine_ValueResp : (val : bool)
													{
														if (val == true)
														{
															 send State_MachineId, State_Machine_SetReq, this, false;
															 receive
															 {
																case State_Machine_SetResp : {}
															 }
														}
													}
												}
											}
										}
									}
									send this, Wakeup;	
								}
							}
						}
					}	
				}
			}
		}
	}
}

