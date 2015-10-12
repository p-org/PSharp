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
		List<Microsoft::PSharp::MachineId^>^ ids;

		NodeManagerConfigEvent(Microsoft::PSharp::MachineId^ env, List<Microsoft::PSharp::MachineId^>^ mids);
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
		ConfigAckEvent();
		~ConfigAckEvent();
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
