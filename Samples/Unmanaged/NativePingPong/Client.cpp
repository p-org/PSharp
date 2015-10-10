#include <iostream>

#include "Client.h"

Client::Client(NetworkEngine* engine)
{
	this->_net_engine = engine;
}

Client::~Client()
{
	delete this->_net_engine;
}

void Client::pong()
{
	std::cout << "Client received a pong.\n" << std::endl;
	this->_net_engine->send();
}
