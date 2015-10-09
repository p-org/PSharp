// PingPongWrapper.h

#pragma once

#include "managed\psharp.h"

using namespace System;

namespace PingPongWrapper {

	public ref class ServerWrapper
		: public Microsoft::PSharp::Interop::Wrapper
	{
	public:
		virtual void invoke(Microsoft::PSharp::Event^ e) override;
	};
}
