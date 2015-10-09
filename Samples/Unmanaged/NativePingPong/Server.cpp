#include <iostream>

#include "Server.h"

Server::Server()
{

}

Server::~Server()
{

}

void Server::ping()
{
	std::cout << "Server received a ping" << std::endl;
}
