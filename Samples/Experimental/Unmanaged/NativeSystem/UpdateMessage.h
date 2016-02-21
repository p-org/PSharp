#pragma once

#include "Message.h"
#include "Log.h"

class UpdateMessage
	: public Message
{
public:
	int idx;
	Log* log;

	UpdateMessage(int idx, Log* log);
	~UpdateMessage();
};