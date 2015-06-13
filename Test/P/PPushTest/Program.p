event eUnit;
event ePing;
event ePong;

main machine Ping {
    var Pong: machine;
	var x: int;

    start state init {
        entry {
            Pong = new Pong(this);
			x = 10;
            raise eUnit;
        }

        on eUnit goto play;
    }

    state play {
		entry {

		}

		on ePong push call1;
    }

    state call1 {
		entry {
			raise eUnit;
		}

		on eUnit goto call2;
    }

    state call2 {
		entry {
			if (x > 0)
			{
				send Pong, ePing;
				x--;
			}
			else
			{
				raise eUnit;
			}
		}

		on eUnit goto end;
    }

    state end {
		entry {

		}
    }
}

machine Pong {
    var Ping : machine;

    start state init {
        entry {
            Ping = payload as machine;
            raise eUnit; 
        }

        on eUnit goto play;
    }

    state play {
		entry {
			send Ping, ePong;
		}

		on ePing goto play;
    }
}
