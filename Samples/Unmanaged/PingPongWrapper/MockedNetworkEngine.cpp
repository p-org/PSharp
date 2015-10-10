#include <iostream>

#include "MockedNetworkEngine.h"
#include "Events.h"

Mocking::MockedNetworkEngine::MockedNetworkEngine(Microsoft::PSharp::Id^ mid)
	: NetworkEngine()
{
	this->_target_machine_id = mid;
}

Mocking::MockedNetworkEngine::~MockedNetworkEngine()
{

}

void Mocking::MockedNetworkEngine::send()
{
	std::cout << "MockedNetworkEngine is sending ..." << std::endl;
	Microsoft::PSharp::Interop::Runtime::send(this->_target_machine_id, gcnew Events::MessageEvent());
}
