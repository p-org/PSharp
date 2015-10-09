#include <iostream>

#include "Client.h"

Client::Client()
{

}

Client::~Client()
{

}

void Client::pong()
{
	std::cout << "Client received a pong" << std::endl;
}
