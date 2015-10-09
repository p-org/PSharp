#include "stdafx.h"
#include "Events.h"
#include "Server.h"
#include "Client.h"

Server::Server()
	: Machine()
{
	this->client = PSharpRuntime::create_machine<Client>();
	this->send(this->client, new Ping());
}

Server::~Server()
{

}