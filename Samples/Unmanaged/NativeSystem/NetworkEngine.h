#pragma once

#include <memory>

#include "Log.h"

class NetworkEngine
{
public:
	NetworkEngine();
	~NetworkEngine();

	virtual void send(int idx, Log* log);
};