#include "ChordNode.p"
#include "ClusterManager.p"

event Config : (machine, seq[int]);
event Local;
event NotifyClientRequest : int;
event NotifyClientResponse : int;

machine Client
{
	var ClusterManager : machine;
	var Keys : seq[int];
	var QueryCounter : int;

	start state Init 
	{
		entry (payload : (machine, seq[int]))
		{
			ClusterManager = payload.0;
			Keys = payload.1;

            // LIVENESS BUG: can never detect the key, and keeps looping without
            // exiting the process. Enable to introduce the bug.
            Keys += (sizeof(Keys), 17);

            QueryCounter = 0;

            raise Local;
		}
		on Local goto Querying;
	}

	state Querying
	{
		entry
		{
			var key : int;
			var temp : bool;

			temp = true;
			if (QueryCounter < 5)
            {
                if (temp == true)
                {
                    key = GetNextQueryKey();
                    send ClusterManager, FindSuccessor, this, key;
                    announce NotifyClientRequest, key;
                }
                else if ($)
                {
                    send ClusterManager, CreateNewNode;
                }
                else
                {
                    send ClusterManager, TerminateNode;
                }
                QueryCounter = QueryCounter + 1;
            }
            raise Local;
		}

		on Local goto Waiting;
	}

	state Waiting
	{
		on Local goto Querying;
		on FindSuccessorResp do (payload :(machine, int))
		{
			var successor : machine;
			var key : int;
			successor =  payload.0;
            key = payload.1;
            announce NotifyClientResponse, key;
            send successor, QueryId, this;
		}

		on QueryIdResp do 
		{
			raise Local;
		}
	}

	fun GetNextQueryKey() : int
	{
		var keyIndex : int;
		var index : int;
		var flag : bool;
		keyIndex = -1;
		while (keyIndex < 0)
		{
			index = 0;
			flag = true;
			while ((index < sizeof(Keys)) && (flag == true))
			{
				if ($)
				{
					keyIndex = index;
					flag = false;
				}
				index = index + 1;
			}
		}
		//return Keys[keyIndex];
		return 17;
	}
}

spec Liveness observes NotifyClientRequest, NotifyClientResponse 
{
	start state Init
	{
		entry
		{
			goto Responded;
		}
	}

	cold state Responded
	{
		on NotifyClientRequest goto Requested;
	}

	hot state Requested
	{
		on NotifyClientResponse goto Responded;
	}
}