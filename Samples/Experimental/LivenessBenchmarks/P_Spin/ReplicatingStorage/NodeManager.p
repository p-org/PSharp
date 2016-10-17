#include "RepairTimer.p"
#include "StorageManager.p"

event NodeManager_ConfigureEvent : (machine, int);
event NotifyFailure : machine;
event ShutDown;
event LocalEvent;

machine NodeManager
{
	var Environment : machine;
	var StorageNodes : seq[machine];
	var NumberOfReplicas : int;
	var StorageNodeMap : map[int, bool];
	var DataMap : map[int, int];
	var RepairTimer : machine;
	var Client : machine;

	start state Init
	{
		entry
		{
			StorageNodes = default(seq[machine]);
            StorageNodeMap = default(map[int, bool]);
            DataMap = default(map[int, int]);

            RepairTimer = new RepairTimer();
            send RepairTimer, RepairTimer_ConfigureEvent, this;
		}
		on NodeManager_ConfigureEvent do (payload : (machine, int))
		{
			var index : int;

			Environment = payload.0;
            NumberOfReplicas = payload.1;

			index = 0;
            while (index < NumberOfReplicas)
            {
                CreateNewNode();
				index = index + 1;
            }

            raise LocalEvent;
		}
		on LocalEvent goto Active;
		defer Client_Request, RepairTimer_Timeout;
	}

	state Active
	{
		on Client_Request do (paylaod : (machine, int))
		{
			var command : int;
			var aliveNodeIds : seq[int];
			var index : int;
			var nodeId : int;

			Client = paylaod.0;
			command = paylaod.1;

			aliveNodeIds = default(seq[int]);
			index = 0;
			while (index < sizeof(StorageNodeMap))
			{	
				if(values(StorageNodeMap)[index] == true)
				{
					aliveNodeIds += (sizeof(aliveNodeIds), keys(StorageNodeMap)[index]);
				}
				index = index + 1;
			}

			index = 0;
			while (index < sizeof(aliveNodeIds))
			{
				nodeId = aliveNodeIds[index];
				send StorageNodes[nodeId], StoreRequest, command;
				index = index + 1;
			}
		}
		on RepairTimer_Timeout do 
		{
			var latestData : int;
			var index : int;
			var numOfReplicas : int;
			var flag : bool;
			var nodeVal : int;

			if (sizeof(DataMap) == 0)
            {
                return;
            }

			latestData = values(DataMap)[0];
			index = 0;
			while (index < sizeof(DataMap))
			{
				if(latestData < values(DataMap)[index])
				{
					latestData = values(DataMap)[index];
				}
				index = index + 1;
			}

            numOfReplicas = 0;
			index = 0;
			while (index < sizeof(DataMap))
			{
				if(values(DataMap)[index] == latestData)
				{
					numOfReplicas = numOfReplicas + 1;
				}
				index = index + 1;
			}
            if (numOfReplicas >= NumberOfReplicas)
            {
                return;
            }

			index = 0;
			flag = true;
			while (index < sizeof(DataMap) && flag == true)
			{
				nodeVal = values(DataMap)[index];
				if (nodeVal != latestData)
                {
                    send StorageNodes[keys(DataMap)[index]], SyncRequest, latestData;
                    numOfReplicas = numOfReplicas + 1;
                }

                if (numOfReplicas == NumberOfReplicas)
                {
                    flag = false;
                }

				index = index + 1;
			}
		}
		on SyncReport do (payload : (int, int))
		{
			var nodeId : int;
			var data : int;

			nodeId = payload.0;
            data = payload.1;

            if (!(nodeId in DataMap))
            {
                DataMap += (nodeId, 0);
            }

            DataMap[nodeId] = data;
		}
		on NotifyFailure do (Node : machine)
		{
			var node : machine;
			var nodeId : int;
			var index : int;

			node = Node;

			index = 0;
			while (index < sizeof(StorageNodes))
			{
				if(StorageNodes[index] == node)
				{
					nodeId = index;
				}
				index = index + 1;
			}

            StorageNodeMap -= nodeId;
			DataMap -= nodeId;

            CreateNewNode();
			
		}
	}

	fun CreateNewNode()
	{	
		var idx : int;
		var node : machine;

		idx = sizeof(StorageNodes);    
		node = new StorageNode();
        StorageNodes += (sizeof(StorageNodes), node);
        StorageNodeMap += (idx, true);
        send node, StorageNode_ConfigureEvent, Environment, this, idx;
	}
}
