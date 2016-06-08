#include "UpdateMessage.h"

UpdateMessage::UpdateMessage(int idx, Log* log)
	: Message()
{
	this->idx = idx;
	this->log = log;
}

UpdateMessage::~UpdateMessage() { }
