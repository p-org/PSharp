#include <iostream>

#include "Server.h"

Server::Server(NetworkEngine* engine)
{
	this->_net_engine = engine;
}

Server::~Server()
{
	delete this->_net_engine;
}

void Server::ping()
{
	std::cout << "Server received a ping.\n" << std::endl;
	this->_net_engine->send();
}
