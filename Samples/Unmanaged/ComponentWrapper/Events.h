#pragma once

#include "managed\psharp.h"
#include "Message.h"

using namespace System;
using namespace System::Collections::Generic;

namespace Events
{
	public ref class NodeManagerConfigEvent
		: public Microsoft::PSharp::Event
	{
	public:
		Microsoft::PSharp::MachineId^ env_id;

		NodeManagerConfigEvent(Microsoft::PSharp::MachineId^ id);
		~NodeManagerConfigEvent();
	};

	public ref class DataNodeConfigEvent
		: public Microsoft::PSharp::Event
	{
	public:
		Microsoft::PSharp::MachineId^ id;
		int idx;

		DataNodeConfigEvent(Microsoft::PSharp::MachineId^ mid, int idx);
		~DataNodeConfigEvent();
	};

	public ref class ConfigAckEvent
		: public Microsoft::PSharp::Event
	{
	public:
		List<Microsoft::PSharp::MachineId^>^ ids;

		ConfigAckEvent(List<Microsoft::PSharp::MachineId^>^ mids);
		~ConfigAckEvent();
	};

	public ref class NodeCreatedEvent
		: public Microsoft::PSharp::Event
	{
	public:
		NodeCreatedEvent();
		~NodeCreatedEvent();
	};

	public ref class FailureEvent
		: public Microsoft::PSharp::Event
	{
	public:
		FailureEvent();
		~FailureEvent();
	};

	public ref class FailedEvent
		: public Microsoft::PSharp::Event
	{
	public:
		int idx;

		FailedEvent(int idx);
		~FailedEvent();
	};

	public ref class MessageEvent
		: public Microsoft::PSharp::Event
	{
	internal:
		Message* msg;
		MessageEvent(Message* msg);

	public:
		MessageEvent();
		~MessageEvent();
	};

	public ref class UnitEvent
		: public Microsoft::PSharp::Event
	{
	public:
		UnitEvent();
		~UnitEvent();
	};
}
