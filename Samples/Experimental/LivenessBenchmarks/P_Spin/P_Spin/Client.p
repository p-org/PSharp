#include "Lk_Machine.p"
#include "RLock_Machine.p"
#include "RWant_Machine.p"
#include "State_Machine.p"

event Sleep;
event Progress;

machine Client
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
			raise Progress;
		}

		on Sleep do 
		{
			var flag : bool;
			flag = true;

			print "Client Sleeping\n";

			send Lk_MachineId, Lk_Machine_AtomicTestSet, this;
			receive 
			{
			    case Lk_Machine_AtomicTestSet_Resp : 
				{
				    while (flag == true)
					{
					    send RLock_MachineId, RLock_Machine_ValueReq, this;
						receive  
						{
						    case RLock_Machine_ValueResp : (payload : bool)
							{
								if(payload == true)
								{
									send RWant_MachineId, RWant_Machine_SetReq, this, true;
									receive
									{
										case RWant_Machine_SetResp : 
										{
										    send State_MachineId, State_Machine_SetReq, this, true;
											receive
											{
												case State_Machine_SetResp : 
												{
													send Lk_MachineId, Lk_Machine_SetReq, this, false;
													receive
													{
														case Lk_Machine_SetResp : 
														{
															announce NotifyClientSleep;
															send State_MachineId, State_Machine_Waiting, this, false;
															receive
															{
																case State_Machine_WaitResp : 
																{
																	announce NotifyClientProgress;
																}
															}	
														}
													}
												}
											}
										}
									}
								}
								else
								{
									flag = false;
								}
							}
						}
					}
					send this, Progress;
				}
			}
		}

		on Progress do
		{
			print "Client Progressing\n";
			send RLock_MachineId, RLock_Machine_ValueReq, this;
			receive 
			{
				case RLock_Machine_ValueResp : (value : bool)
				{
					assert (value == false);
					send RLock_MachineId, RLock_Machine_SetReq, this, true;
					receive 
					{
						case RLock_Machine_SetResp : 
						{
							send Lk_MachineId, Lk_Machine_SetReq, this, false;
							receive 
							{
								case Lk_Machine_SetResp : 
								{
									send this, Sleep;
								}
							}
						}
					}
				}
			}
		}
	}
}

event NotifyClientSleep;
event NotifyClientProgress;

spec Liveness observes NotifyClientProgress, NotifyClientSleep 
{
	start state Init
	{
		entry
		{
			goto Progressing;
		}
	}

	cold state Progressing
	{
		on NotifyClientSleep goto Suspended;
	}

	hot state Suspended
	{
		on NotifyClientProgress goto Progressing;
	}
}

