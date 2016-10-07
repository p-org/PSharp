event ChordNode_Config : (int, seq[int], seq[machine], seq[int], machine);
event Join : (int, seq[machine], seq[int], int, machine);
event FindSuccessor : (machine, int);
event FindSuccessorResp : (machine, int);
event FindPredecessor : machine;
event FindPredecessorResp : machine;
event QueryId : machine;
event QueryIdResp : int;
event AskForKeys : (machine, int);
event AskForKeysResp : seq[int];
event NotifySuccessor : machine;
event JoinAck;
event Stabilize;
event Terminate;

type Finger = (StartId : int, EndId : int, Node : machine);

machine ChordNode
{
	var NodeId : int;
	var Keys : seq[int];
	var NumOfIds : int;
	var FingerTable : map[int, Finger];
	var Predecessor : machine;
	var Manager : machine;

	start state Init
	{
		entry
		{
			FingerTable = default(map[int, Finger]);
		}
		on Local goto Waiting;
		on ChordNode_Config do (payload : (int, seq[int], seq[machine], seq[int], machine))
		{
			var nodes : seq[machine];
			var nodeIds : seq[int];
			var index : int;
			var flag : bool;
			var startId : int;
			var end : int;
			var temp : int;
			var nodeId : int;
			var tempIndex : int;
			var x : int;
			var y : int;
			var z : int;
			var finger : Finger;
			
			NodeId = payload.0;
			Keys = payload.1;
			Manager = payload.4;
			nodes = payload.2;
			nodeIds = payload.3;
	
			NumOfIds = 1;
			index = 0;
			while (index < sizeof(nodes))
			{
				NumOfIds = NumOfIds * 2;
				index = index + 1;
			}
			
			index = 1;
			while (index <= sizeof(nodes))
			{
				temp = 1;
				tempIndex = 1;
				while(tempIndex <=  (index - 1))
				{
					temp = temp * 2;
					tempIndex = tempIndex + 1;
				}
				
				x = (NodeId + temp) / NumOfIds;
				y = NumOfIds * x;
				z = (NodeId + temp) - y;
				startId = z;
				
				temp = 1;
				tempIndex = 1;
				while(tempIndex <=  index)
				{
					temp = temp * 2;
					tempIndex = tempIndex + 1;
				}
				x = (NodeId + temp) / NumOfIds;
				y = NumOfIds * x;
				z = (NodeId + temp) - y;
				end  = z;
					
				nodeId = GetSuccessorNodeId(startId, nodeIds);
				finger = default(Finger);
				finger.StartId = startId;
				finger.EndId = end;
				finger.Node = nodes[nodeId];
				FingerTable += (startId, finger);
				index = index + 1;
			}
			index = 0;
			flag = true;
			while (index < sizeof(nodeIds) && flag == true)
			{
	            if (nodeIds[index] == NodeId)
				{
	                Predecessor = nodes[WrapSubtract(index, 1, sizeof(nodeIds))];
					flag = false;
				}
				index = index + 1;
			}
			raise Local;
		}
		on Join do (payload : (int, seq[machine], seq[int], int, machine))
		{
			var nodes : seq[machine];
			var nodeIds : seq[int];
			var index : int;
			var startId : int;
			var temp : int;
			var tempIndex : int;
			var end : int;
			var nodeId : int;
			var successor : machine;
			var x : int;
			var y : int;
			var z : int;
			var finger : Finger;

			NodeId = payload.0;
            Manager = payload.4;
            NumOfIds = payload.3;
			nodes = payload.1;
			nodeIds = payload.2;
			index = 1;
            while (index <= sizeof(nodes))
            {
				temp = 1;
				tempIndex = 1;
				while(tempIndex <=  (index - 1))
				{
					temp = temp * 2;
					tempIndex = tempIndex + 1;
				}
				x = (NodeId + temp) / NumOfIds;
				y = NumOfIds * x;
				z = (NodeId + temp) - y;
				startId = z;

				temp = 1;
				tempIndex = 1;
				while(tempIndex <=  index)
				{
					temp = temp * 2;
					tempIndex = tempIndex + 1;
				}
				x = (NodeId + temp) / NumOfIds;
				y = NumOfIds * x;
				z = (NodeId + temp) - y;
                end = z;

				nodeId = GetSuccessorNodeId(startId, nodeIds);
				finger = default(Finger);
				finger.StartId = startId;
				finger.EndId = end;
				finger.Node = nodes[nodeId];
                FingerTable += (startId, finger);
				index = index + 1;
            }
			x = (NodeId + 1) / NumOfIds;
			y = NumOfIds * x;
			z = (NodeId + 1) - y;
			successor = FingerTable[z].Node;

            send Manager, JoinAck;
            send successor, NotifySuccessor, this;
		}
		defer AskForKeys, NotifySuccessor, Stabilize;
	}

	state Waiting
	{
		on FindSuccessor do (payload: (machine, int))
		{
			var sender : machine;
			var key : int;
			var idToAsk : int;
			var index : int;
			var flag : bool;
			var x : int;
			var y : int;
			var z : int;
			var tIndex : int;
			var tFlag : bool;
			var w : int;

			sender = payload.0;
			key = payload.1;
			index = 0;
			flag = true;
			
			while (index < sizeof(Keys) && flag == true)
			{
				if(key == Keys[index])
				{
					send sender, FindSuccessorResp, this, key;
					flag = false;
				}
				index = index + 1;
			}
			if(flag == true)
			{
				if (key in FingerTable)
				{
					w = (NodeId + 1) / NumOfIds;
					y = NumOfIds * x;
					z = (NodeId + 1) - y;
	                send sender, FindSuccessorResp, FingerTable[z].Node, key;
				}
				else
				{
					idToAsk = -1;
					index = 0;
					while (index < sizeof(FingerTable))
					{
						if (((values(FingerTable)[index].StartId > values(FingerTable)[index].EndId) &&
							(values(FingerTable)[index].StartId <= key || key < values(FingerTable)[index].EndId)) ||
							((values(FingerTable)[index].StartId < values(FingerTable)[index].EndId) &&
	                        (values(FingerTable)[index].StartId <= key && key < values(FingerTable)[index].EndId)))
						{
	                        idToAsk = keys(FingerTable)[index];
						}
	
						index = index + 1;
					}
	
					if (idToAsk < 0)
					{
						x = (NodeId + 1) / NumOfIds;
						y = NumOfIds * x;
						z = (NodeId + 1) - y;
	                    idToAsk = z;
					}
	
					if (FingerTable[idToAsk].Node == this)
					{
						index = 0;
						flag = true;
						while (index < sizeof(FingerTable) && flag == true)
						{
							if (values(FingerTable)[index].EndId == idToAsk ||
	                            values(FingerTable)[index].EndId == idToAsk - 1)
							{
								idToAsk = keys(FingerTable)[index];
								flag = false;
							}
							index = index + 1;
						}

						assert (!(FingerTable[idToAsk].Node == this));
					}

					send FingerTable[idToAsk].Node, FindSuccessor, sender, key;
				}
			}
		}

		on FindSuccessorResp do (payload: (machine, int))
		{
			var successor : machine;
			var key : int;
			var flag : bool;
			var index : int;
			var finger : Finger;

			successor = payload.0;
			key = payload.1;

			flag = false;
			index = 0;
			while (index < sizeof(FingerTable))
			{
				if(key == keys(FingerTable)[index])
				{
					flag = true;
				}
				index = index + 1;
			}
            assert (flag == true);

			finger = default(Finger);
			finger.StartId = FingerTable[key].StartId;
			finger.EndId = FingerTable[key].EndId;
			finger.Node = successor;
            FingerTable[key] = finger;
		}

		on FindPredecessor do (Sender : machine)
		{
			var sender : machine; 
			sender = Sender;
            if (Predecessor != null)
            {
                send sender, FindPredecessorResp, Predecessor;
            }
		}

		on FindPredecessorResp do (Node : machine)
		{
			var successor : machine;
			var x : int;
			var y : int;
			var z : int;
			var finger : Finger;

			successor = Node;
            if (!(successor == this))
            {
				x = (NodeId + 1) / NumOfIds;
				y = NumOfIds * x;
				z = (NodeId + 1) - y;
				finger = default(Finger);
				finger.StartId = FingerTable[z].StartId;
				finger.EndId = FingerTable[z].EndId;
				finger.Node = successor;
                FingerTable[z] = finger;

                send successor, NotifySuccessor, this;
                send successor, AskForKeys, this, NodeId;
            }
		}

		on QueryId do (Sender : machine)
		{
			var sender : machine;
			sender = Sender;
            send sender, QueryIdResp, NodeId;
		}

		on AskForKeys do (payload: (machine, int))
		{
			var sender : machine;
			var senderId : int;
			var keysToSend : seq[int];
			var index : int;
			var key : int;

			sender = payload.0;
			senderId = payload.1;
            assert (Predecessor == sender);

            
			keysToSend = default(seq[int]);

			
			index = 0;
            while (index < sizeof(Keys))
            {
				key = Keys[index];
                if (key <= senderId)
                {
                    keysToSend += (sizeof(keysToSend), key);
                }
				index = index + 1;
            }

            if (sizeof(keysToSend) > 0)
            {
				index = 0;
				while (index < sizeof(Keys))
				{
					key = Keys[index];
					Keys -= key;
					index = index + 1;
				}

                send sender, AskForKeysResp, keysToSend;
            }
		}

		on AskForKeysResp do (Keys : seq[int])
		{
			var keysVar : seq[int];
			var index : int;
			var key : int;

			keysVar = Keys;
			index = 0;
            while (index < sizeof(keysVar))
            {
				
				key = Keys[index];
                Keys += (sizeof(Keys), key);

				index = index + 1;
            }
		}

		on NotifySuccessor do (Node : machine)
		{
			var predecessor : machine;
			predecessor = Node;
            if (!(predecessor == this))
            {
                Predecessor = predecessor;
            }
		}

		on Stabilize do 
		{
			var successor : machine;
			var index : int;
			var x : int;
			var y : int;
			var z : int;

			x = (NodeId + 1) / NumOfIds;
			y = NumOfIds * x;
			z = (NodeId + 1) - y;
			successor = FingerTable[z].Node;
            send successor, FindPredecessor, this;
			index = 0;
            while (index < sizeof(FingerTable))
            {
                if (!(values(FingerTable)[index].Node == successor))
                {
                    send successor, FindSuccessor, this, keys(FingerTable)[index];
                }
				index = index + 1;
            }
		}

		on Terminate do
		{
			raise halt;
		}
	}
}

fun GetSuccessorNodeId(startId: int, nodeIds : seq[int]) : int
{
	var candidate : int;
	var index : int;
	var id : int;
	var flag : bool;

	candidate = -1;
	index = 0;
	while (index < sizeof(nodeIds))
	{
		id = nodeIds[index];
		if (id >= startId)
		{
			if(candidate < 0 || id < candidate)
			{
				candidate = id;
			}
		}

		index = index + 1;
	}
	if (candidate < 0)
    {
		index = 0;
		while (index < sizeof(nodeIds))
		{
			id = nodeIds[index];
			if (id < startId)
			{
				if(candidate < 0 || id < candidate)
				{
					candidate = id;
				}
			}

			index = index + 1;
		}
    }
	index = 0;
	flag = true;
	while (index < sizeof(nodeIds) && flag == true)
    {
		if (nodeIds[index] == candidate)
		{
			candidate = index;
			flag = false;
		}
		index = index + 1;
	}
	return candidate;
}

fun WrapSubtract(left : int, right : int, ceiling : int) : int
{
	var result : int;
	result = left - right;
	if (result < 0)
	{
		result = ceiling + result;
	}
	return result;
}
