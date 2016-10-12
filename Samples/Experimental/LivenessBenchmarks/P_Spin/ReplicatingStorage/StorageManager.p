#include "SyncTimer.p"

event StorageNode_ConfigureEvent : (machine, machine, int);
event StoreRequest : int;
event SyncReport : (int, int);
event SyncRequest : int;

event LivenessMonitor_NotifyNodeCreated : int;
event LivenessMonitor_NotifyNodeUpdate : (int, int);
event LivenessMonitor_NotifyNodeFail : int;

machine StorageNode
{
	var Environment : machine;
	var NodeManager : machine;
	var NodeId : int;
	var Data : int;
	var SyncTimer : machine;

	start state Init
	{
		entry
		{
			Data = 0;
            SyncTimer = new SyncTimer();
            send SyncTimer, SyncTimer_ConfigureEvent, this;
		}
		on StorageNode_ConfigureEvent do (payload : (machine, machine, int))
		{
			Environment = payload.0;
            NodeManager = payload.1;
            NodeId = payload.2;

            announce LivenessMonitor_NotifyNodeCreated, NodeId;
            send Environment, NotifyNode, this;

            raise LocalEvent;
		}
		on LocalEvent goto Active;
		defer SyncTimer_Timeout;
	}

	state Active
	{	
		on StoreRequest do (Command : int)
		{
			var cmd : int;

			cmd = Command;
            Data = Data + cmd;
            
            announce LivenessMonitor_NotifyNodeUpdate, NodeId, Data;
		}
		on SyncRequest do (dat : int)
		{
			var data : int;
			data = dat;

            Data = data;
           
            announce LivenessMonitor_NotifyNodeUpdate, NodeId, Data;
		}
		on SyncTimer_Timeout do 
		{
			send NodeManager, SyncReport, NodeId, Data;
		}
		on FaultInject do 
		{
			announce LivenessMonitor_NotifyNodeFail, NodeId;
            send SyncTimer, halt;
            raise halt;
		}
	}
}

spec Liveness observes LivenessMonitor_ConfigureEvent, LivenessMonitor_NotifyNodeCreated, 
	LivenessMonitor_NotifyNodeFail, LivenessMonitor_NotifyNodeUpdate
{
	var DataMap : map[int, int];
	var NumberOfReplicas : int;

	start state Init
	{
		entry
		{
			DataMap = default(map[int, int]);
		}
		on LivenessMonitor_ConfigureEvent do (numberOfReplicas : int)
		{
			NumberOfReplicas = numberOfReplicas;
            raise LocalEvent;
		}
		on LocalEvent goto Repaired;
	}

	cold state Repaired
	{
		on LivenessMonitor_NotifyNodeCreated do (NodeId : int)
		{
			var nodeId : int;
			nodeId = NodeId;
            DataMap += (nodeId, 0);
		}
		on LivenessMonitor_NotifyNodeFail do (NodeId : int)
		{
			var nodeId : int;
			nodeId = NodeId;
			DataMap -= nodeId;
            raise LocalEvent;
		}
		on LivenessMonitor_NotifyNodeUpdate do (payload : (int, int)) 
		{
			var nodeId : int;
			var data : int;

			nodeId = payload.0;
            data = payload.1;
            DataMap[nodeId] = data;
		}
		on LocalEvent goto Repairing;
	}

	hot state Repairing
	{
		on LivenessMonitor_NotifyNodeCreated do (NodeId : int)
		{
			var nodeId : int;
			nodeId = NodeId;
            DataMap += (nodeId, 0);
		}
		on LivenessMonitor_NotifyNodeFail do (NodeId : int)
		{
			var nodeId : int;
			nodeId = NodeId;
			DataMap -= nodeId;
            raise LocalEvent;
		}
		on LivenessMonitor_NotifyNodeUpdate do (payload : (int, int))
		{
			var numOfReplicas : int;
			var nodeId : int;
			var data : int;
			var consensus :int;
			var index : int;
			var dataValues : seq[int];
			var dataCount : map[int, int];

			nodeId = payload.0;
            data = payload.1;
            DataMap[nodeId] = data;

			dataValues = default(seq[int]);
			index = 0;
			while (index < sizeof(DataMap))
			{
				dataValues += (sizeof(dataValues), values(DataMap)[index]);
				index = index + 1;
			}

			dataCount = default(map[int, int]);
			index = 0;
			while (index < sizeof(dataValues))
			{
				if(dataValues[index] in dataCount)
				{
					dataCount[dataValues[index]] = dataCount[dataValues[index]] + 1;
				}
				else
				{	
					dataCount += (dataValues[index], 1);
				}
				index = index + 1;
			}

			consensus = 0;
			index = 0;
			while (index < sizeof(dataCount))
			{
				if(consensus < values(dataCount)[index])
				{
					consensus = values(dataCount)[index];
				}
				index = index + 1;
			}

            numOfReplicas = consensus;
            if (numOfReplicas >= NumberOfReplicas)
            {
                raise LocalEvent;
            }
		}
		on LocalEvent goto Repaired;
	}
}
