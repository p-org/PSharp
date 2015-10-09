#pragma once

#include <memory>
#include "psharp.h"

class Server
	: public Machine
{
private:
	Id* client;

public:
	Server();
	~Server();
};