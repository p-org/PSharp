#include <iostream>

#include "MockedNetworkEngine.h"
#include "Events.h"
#include "UpdateMessage.h"

Mocking::MockedNetworkEngine::MockedNetworkEngine(List<Microsoft::PSharp::MachineId^>^ mids)
	: NetworkEngine()
{
	this->_target_machine_ids = mids;
}

Mocking::MockedNetworkEngine::~MockedNetworkEngine()
{

}

void Mocking::MockedNetworkEngine::send(int idx, Log* log)
{
	auto enumerator = this->_target_machine_ids->GetEnumerator();
	while (enumerator.MoveNext())
	{
		std::cout << "MockedNetworkEngine is sending to data node " << enumerator.Current->Value << std::endl;
		auto msg = std::make_shared<UpdateMessage>(idx, log);
		Microsoft::PSharp::Interop::Runtime::send(enumerator.Current, gcnew Events::MessageEvent(msg.get()));
	}
}
