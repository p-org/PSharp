#pragma once

#include "managed\psharp.h"

using namespace System;

namespace Events
{
	public ref class ConfigEvent
		: public Microsoft::PSharp::Event
	{
	public:
		Microsoft::PSharp::MachineId^ id;

		ConfigEvent(Microsoft::PSharp::MachineId^ mid);
		~ConfigEvent();
	};

	public ref class MessageEvent
		: public Microsoft::PSharp::Event
	{
	public:
		MessageEvent();
		~MessageEvent();
	};
}
