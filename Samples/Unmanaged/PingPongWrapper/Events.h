#pragma once

#include "managed\psharp.h"

using namespace System;

namespace Events {

	public ref class MessageEvent
		: public Microsoft::PSharp::Event
	{
	public:
		MessageEvent();
		~MessageEvent();
	};
}
