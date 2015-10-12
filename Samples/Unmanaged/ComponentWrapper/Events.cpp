#include <memory>

#include "Events.h"

Events::NodeManagerConfigEvent::NodeManagerConfigEvent(Microsoft::PSharp::MachineId^ id)
	: Microsoft::PSharp::Event()
{
	this->env_id = id;
}

Events::NodeManagerConfigEvent::~NodeManagerConfigEvent() { }

Events::DataNodeConfigEvent::DataNodeConfigEvent(Microsoft::PSharp::MachineId^ mid, int idx)
	: Microsoft::PSharp::Event()
{
	this->id = mid;
	this->idx = idx;
}

Events::DataNodeConfigEvent::~DataNodeConfigEvent() { }

Events::ConfigAckEvent::ConfigAckEvent(List<Microsoft::PSharp::MachineId^>^ mids)
	: Microsoft::PSharp::Event()
{
	this->ids = mids;
}

Events::ConfigAckEvent::~ConfigAckEvent() { }

Events::NodeCreatedEvent::NodeCreatedEvent()
	: Microsoft::PSharp::Event()
{

}

Events::NodeCreatedEvent::~NodeCreatedEvent() { }

Events::FailureEvent::FailureEvent()
	: Microsoft::PSharp::Event()
{

}

Events::FailureEvent::~FailureEvent() { }

Events::FailedEvent::FailedEvent(int idx)
	: Microsoft::PSharp::Event()
{
	this->idx = idx;
}

Events::FailedEvent::~FailedEvent() { }

Events::MessageEvent::MessageEvent()
	: Microsoft::PSharp::Event()
{

}

Events::MessageEvent::MessageEvent(Message* msg)
	: Microsoft::PSharp::Event()
{
	this->msg = msg;
}

Events::MessageEvent::~MessageEvent() { }

Events::UnitEvent::UnitEvent()
	: Microsoft::PSharp::Event()
{

}

Events::UnitEvent::~UnitEvent() { }