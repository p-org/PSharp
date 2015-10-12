#include <memory>

#include "Events.h"

Events::NodeManagerConfigEvent::NodeManagerConfigEvent(Microsoft::PSharp::MachineId^ env, List<Microsoft::PSharp::MachineId^>^ mids)
	: Microsoft::PSharp::Event()
{
	this->env_id = env;
	this->ids = mids;
}

Events::NodeManagerConfigEvent::~NodeManagerConfigEvent() { }

Events::DataNodeConfigEvent::DataNodeConfigEvent(Microsoft::PSharp::MachineId^ mid, int idx)
	: Microsoft::PSharp::Event()
{
	this->id = mid;
	this->idx = idx;
}

Events::DataNodeConfigEvent::~DataNodeConfigEvent() { }

Events::ConfigAckEvent::ConfigAckEvent()
	: Microsoft::PSharp::Event()
{

}

Events::ConfigAckEvent::~ConfigAckEvent() { }

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