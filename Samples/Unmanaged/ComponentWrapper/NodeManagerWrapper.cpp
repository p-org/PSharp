#include <memory>

#include "NodeManagerWrapper.h"
#include "NodeManager.h"
#include "MockedNetworkEngine.h"
#include "Events.h"
#include "UpdateMessage.h"

public class NodeManagerNativeComponents
{
public:
	Mocking::MockedNetworkEngine* mocked_net_engine;
	NodeManager* node_manager;
};

Mocking::NodeManagerWrapper::NodeManagerWrapper(List<Microsoft::PSharp::MachineId^>^ mids)
{
	this->_native_components = new NodeManagerNativeComponents();
	this->_native_components->mocked_net_engine = new Mocking::MockedNetworkEngine(mids);
	this->_native_components->node_manager = new NodeManager(
		this->_native_components->mocked_net_engine);
}

Mocking::NodeManagerWrapper::~NodeManagerWrapper()
{
	delete this->_native_components;
}

void Mocking::NodeManagerWrapper::invoke(Microsoft::PSharp::Event^ e)
{
	if (e->GetType() == Events::MessageEvent::typeid)
	{
		auto msg_event = dynamic_cast<Events::MessageEvent^>(e);
		if (auto msg = dynamic_cast<UpdateMessage*>(msg_event->msg))
		{
			this->_native_components->node_manager->store_log(msg->idx, msg->log);
		}
	}
}
