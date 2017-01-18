event Local;
event NotifyMessage;
event NotifyDone;

machine Main
{
	start state Init
	{
		entry
		{
			goto Waiting;
		}
	}

	state Waiting
	{
		entry
		{
			send this, Local;
            announce NotifyMessage;
            if($)
            {
                announce NotifyDone;
            }
		}
		on Local do 
		{
			send this, Local;
            announce NotifyMessage;
            if($)
            {
                announce NotifyDone;
            }
		}
	}
}

spec LivenessMonitor observes NotifyMessage, NotifyDone
{
	start state Init
	{
		on NotifyMessage goto HotState;
	}

	hot state HotState
	{
		on NotifyMessage goto HotState;
		on NotifyDone goto ColdState;
	}

	cold state ColdState
	{
		on NotifyMessage goto HotState;
	}
}