#pragma once

#include "psharp.h"

class Ping
	: public Event
{
public:
	Ping();
	~Ping();
};

class Pong
	: public Event
{
public:
	Pong();
	~Pong();
};