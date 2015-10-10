#pragma once

#include "managed\psharp.h"

using namespace System;

class ServerNativeComponents;

namespace PingPongWrapper {

	public ref class ServerWrapper
		: public Microsoft::PSharp::Interop::Wrapper
	{
	private:
		ServerNativeComponents* _server_native_components;

	public:
		ServerWrapper();
		~ServerWrapper();
		virtual void invoke(Microsoft::PSharp::Event^ e) override;
	};
}
