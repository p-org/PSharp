#include "NodeManager.p"
#include "Client.p"
#include "FailureTimer.p"

event NotifyNode : machine;
event FaultInject;
event CreateFailure;
event LivenessMonitor_ConfigureEvent : int;

machine Main
{
	var NodeManager : machine;
	var NumberOfReplicas : int;
	var AliveNodes : seq[machine];
	var NumberOfFaults : int;
	var Client : machine;
	var FailureTimer : machine;

	start state Init
	{
		entry
		{
			NumberOfReplicas = 3;
            NumberOfFaults = 1;
            AliveNodes = default(seq[machine]);

            announce LivenessMonitor_ConfigureEvent, NumberOfReplicas;

            NodeManager = new NodeManager();
            Client = new Client();

            raise LocalEvent;
		}
		on LocalEvent goto Configuring;
	}

	state Configuring
	{	
		entry
		{
			send NodeManager, NodeManager_ConfigureEvent, this, NumberOfReplicas;
            send Client, Client_ConfigureEvent, NodeManager;
            raise LocalEvent;
		}
		on LocalEvent goto Active;
		defer FailureTimer_Timeout;
	}

	state Active
	{
		on NotifyNode do (Node : machine)
		{
			var node : machine;
			node = Node;

            AliveNodes += (sizeof(AliveNodes), node);

            if (sizeof(AliveNodes) == NumberOfReplicas && FailureTimer == null)
            {
                FailureTimer = new FailureTimer();
                send FailureTimer, FailureTimer_ConfigureEvent, this;

				raise FailureTimer_Timeout;
            }
		}
		on FailureTimer_Timeout do
		{
			var nodeId : int;
			var node : machine;
			var index : int;
			var flag : bool;

			if (NumberOfFaults == 0 || sizeof(AliveNodes) == 0)
            {
                return;
            }

			index = 0;
			flag = false;
			while(index < sizeof(AliveNodes) && flag == false)
			{
				if($)
				{
					nodeId = index;
					flag = true;
				}
				index = index + 1;
			}

			node = AliveNodes[nodeId];

            send node, FaultInject;
            send NodeManager, NotifyFailure, node;
            AliveNodes -= nodeId;

            NumberOfFaults = NumberOfFaults - 1;
            if (NumberOfFaults == 0)
            {
                send FailureTimer, halt;
            }
		}
	}
}