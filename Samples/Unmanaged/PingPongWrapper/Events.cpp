#include <memory>

#include "Events.h"

Events::ConfigEvent::ConfigEvent(Microsoft::PSharp::MachineId^ mid)
	: Microsoft::PSharp::Event()
{
	this->id = mid;
}

Events::ConfigEvent::~ConfigEvent() { }

Events::MessageEvent::MessageEvent()
	: Microsoft::PSharp::Event()
{

}

Events::MessageEvent::~MessageEvent() { }