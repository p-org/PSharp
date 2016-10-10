event Local;
event Search : int;

event SearchStarted;
event SearchFinished;

machine Main
{
	var store : seq[int];

	start state Init
	{
		entry
		{
			store = default(seq[int]);
			store += (sizeof(store), 8);
			store += (sizeof(store), 18);
			store += (sizeof(store), 28);

			raise Local;
		}
		on Local goto Waiting;
	}

	state Waiting
	{
		entry
		{
			send this, Search, 30;
			announce SearchStarted;
		}
		on Search do (payload : int)
		{
			var flag : bool;
			var index : int;

			flag = false;
			index = 0;
			while(index < sizeof(store) && flag == false)
			{
				if (payload == store[index])
				{
					flag = true;
				}
				index = index + 1;
			}

			if(flag == true)
			{
				raise halt;
				announce SearchFinished;
			}
			else
			{
				send this, Search, payload;
			}
		}
	}
}

spec liveness observes SearchStarted, SearchFinished
{
	start state Init
	{
		entry
		{
			goto Searched;
		}
	}

	cold state Searched
	{
		on SearchStarted  do
		{
			assert false;
			goto Searching;
		}
	}

	hot state Searching
	{
		on SearchFinished do 
		{
			assert false;
			goto Searched;
		}
	}
}