#include <iostream>

#include "MockedNetworkEngine.h"

Mocking::MockedNetworkEngine::MockedNetworkEngine()
{

}

Mocking::MockedNetworkEngine::~MockedNetworkEngine()
{

}

void Mocking::MockedNetworkEngine::send()
{
	std::cout << "MockedNetworkEngine is sending ..." << std::endl;
}
