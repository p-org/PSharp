#pragma once

#include <memory>

#include "NetworkEngine.h"

class Server
{
private:
	NetworkEngine* _net_engine;

public:
	Server(NetworkEngine* engine);
	~Server();

	void ping();
};