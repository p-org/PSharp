#pragma once

#include <memory>

#include "NetworkEngine.h"

class Client
{
private:
	NetworkEngine* _net_engine;

public:
	Client(NetworkEngine* engine);
	~Client();

	void pong();
};