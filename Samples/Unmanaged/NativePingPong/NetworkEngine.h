#pragma once

#include <memory>

class NetworkEngine
{
public:
	NetworkEngine();
	~NetworkEngine();

	virtual void send();
};