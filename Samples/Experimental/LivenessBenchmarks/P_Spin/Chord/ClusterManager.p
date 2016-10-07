#include "Client.p"

event CreateNewNode;
event TerminateNode;

machine Main
{
	var NumOfNodes : int;
	var NumOfIds : int;
	var ChordNodes : seq[machine];
	var Keys : seq[int];
	var NodeIds : seq[int];
	var Client : machine;

	start state Init
	{
		entry
		{
			var index : int;
			var x : seq[int];
			var y : seq[machine];
			var z : seq[int];
			var w : seq[int];
			var nodeKeys : map[int, seq[int]]; 
			var keysVar : seq[int];
			var m : machine;
			var tIndex : int;

			NumOfNodes = 3;
			index = 0;
			NumOfIds = 1;
			while (index < NumOfNodes)
			{
				NumOfIds = NumOfIds * 2;
				index = index + 1;
			}
            ChordNodes = default(seq[machine]);

            NodeIds = default(seq[int]); 
			NodeIds += (sizeof(NodeIds), 0);
			NodeIds += (sizeof(NodeIds), 1);
			NodeIds += (sizeof(NodeIds), 3);

			Keys = default(seq[int]);
			Keys += (sizeof(Keys), 1);
			Keys += (sizeof(Keys), 2);
			Keys += (sizeof(Keys), 6);

			index = 0;
            while (index < sizeof(NodeIds))
            {
				m = new ChordNode();
                ChordNodes += (sizeof(ChordNodes), m);
				index = index + 1;
            }

			
			nodeKeys = AssignKeysToNodes();
			
			index = 0;
            while (index < sizeof(ChordNodes))
            {
				keysVar = nodeKeys[NodeIds[index]];
				
				x = default(seq[int]);
				tIndex = 0;
				while (tIndex < sizeof(keysVar))
				{
					x += (sizeof(x), keysVar[tIndex]);
					tIndex = tIndex + 1;
				}
				y = default(seq[machine]);
				tIndex = 0;
				while (tIndex < sizeof(ChordNodes))
				{
					y += (sizeof(y), ChordNodes[tIndex]);
					tIndex = tIndex + 1;
				}
				z = default(seq[int]);
				tIndex = 0;
				while (tIndex < sizeof(NodeIds))
				{
					z += (sizeof(z), NodeIds[tIndex]);
					tIndex = tIndex + 1;
				}
				
                send ChordNodes[index], ChordNode_Config, NodeIds[index], x, y, z, this;
				
				index = index + 1;
            }
			w = default(seq[int]);
			tIndex = 0;
			while (tIndex < sizeof(Keys))
			{
				w += (sizeof(w), Keys[tIndex]);
				tIndex = tIndex + 1;
			}
            Client = new Client(this, z);

            raise Local;
		}	

		on Local goto Waiting;
	}

	state Waiting
	{
		on FindSuccessor do (payload : (machine, int))
		{
			send ChordNodes[0], FindSuccessor, payload.0, payload.1;
		}

		on CreateNewNode do 
		{
			var index : int;
			var newId : int;
			var newNode : machine;
			var x : seq[machine];
			var y : seq[int];
			var flag : bool;
			var tIndex : int;

			flag = false;
			index = 0;
			while (index < sizeof(NodeIds))
			{
				if(newId == NodeIds[index])
				{
					 flag = true;
				}
				index = index + 1;
			}

			newId = -1;
            while ((newId < 0 || flag == true) && sizeof(NodeIds) < NumOfIds)
            {
				index = 0;
                while (index < NumOfIds)
                {
                    if ($)
                    {
                        newId = index;
                    }
					index = index + 1;
                }
            }

            assert (newId >= 0);

            
			newNode = new ChordNode();

            NumOfNodes = NumOfNodes + 1;
            NodeIds += (sizeof(NodeIds), newId);
            ChordNodes += (sizeof(ChordNodes), newNode);

			x = default(seq[machine]);
			tIndex = 0;
			while (tIndex < sizeof(ChordNodes))
			{
				x += (sizeof(x), ChordNodes[tIndex]);
				tIndex = tIndex + 1;
			}
			y = default(seq[int]);
			tIndex = 0;
			while (tIndex < sizeof(NodeIds))
			{
				y += (sizeof(y), NodeIds[tIndex]);
				tIndex = tIndex + 1;
			}
            send newNode, Join, newId, x, y, NumOfIds, this;
		}

		on TerminateNode do 
		{
			var endId : int;
			var index : int;
			var endNode : machine;
			var flag : bool;
			var tIndex : int;

			tIndex = 0;
			flag = false;
			while (tIndex < sizeof(NodeIds))
			{
				if(endId == NodeIds[tIndex])
				{
					flag = true;
				}
				tIndex = tIndex + 1;
			}

			endId  = -1;
            while (endId < 0 || !(flag == true) &&
                sizeof(NodeIds) > 0)
            {
				index = 0;
                while (index < sizeof(ChordNodes))
                {
                    if ($)
                    {
                        endId = index;
                    }
					index = index  + 1;
                }
            }

            assert (endId >= 0);
  
			endNode = ChordNodes[endId];

            NumOfNodes = NumOfNodes - 1;
            NodeIds -= endId;
            ChordNodes -= endId;

            send endNode, Terminate;
		}

		on JoinAck do 
		{
			var index : int;
			index = 0;
			while (index < sizeof(ChordNodes))
            {
                send ChordNodes[index], Stabilize;
				index = index + 1;
            }
		}
	}

 fun AssignKeysToNodes() : map[int, seq[int]]
 {
	var nodeKeys : map[int, seq[int]]; 
	var index : int;
	var assigned : bool;
	var jIndex : int;
	var tIndex : int;
	var flag : bool;

	nodeKeys = default(map[int, seq[int]]);
	index = sizeof(Keys) - 1;
    while (index >= 0)
    {
		assigned = false;
		jIndex = 0;
        while (jIndex < sizeof(NodeIds) && !assigned)
        {
			if (Keys[index] <= NodeIds[jIndex])
			{
				tIndex = 0;
				flag = false;
				while (tIndex < sizeof(nodeKeys))
                {
					if(NodeIds[jIndex] == keys(nodeKeys)[tIndex])
					{
						nodeKeys[NodeIds[jIndex]] += (sizeof(nodeKeys[NodeIds[jIndex]]), Keys[index]);
						flag = true;
					}
					tIndex = tIndex + 1;
				}
				if(flag == false)
				{
					nodeKeys += (NodeIds[jIndex], default(seq[int]));
					nodeKeys[NodeIds[jIndex]] += (sizeof(nodeKeys[NodeIds[jIndex]]), Keys[index]);
                }
				assigned = true;
			}
			jIndex = jIndex + 1;
		}
		if (!assigned)
        {
			tIndex = 0;
			flag = false;
			while (tIndex < sizeof(nodeKeys))
			{
				if(NodeIds[0] == keys(nodeKeys)[tIndex])
				{
					nodeKeys[NodeIds[0]] += (sizeof(nodeKeys[NodeIds[0]]), Keys[index]);
					flag = true;
				}
				tIndex = tIndex + 1;
			}
			if (flag == false)
			{
				nodeKeys += (NodeIds[0], default(seq[int]));
				nodeKeys[NodeIds[0]] += (sizeof(nodeKeys[NodeIds[0]]), Keys[index]);
            }
		}
		index = index - 1;
	}
	return nodeKeys;
  }
}

