#pragma once

#include "managed\psharp.h"

using namespace System;

class ClientNativeComponents;

namespace PingPongWrapper {

	public ref class ClientWrapper
		: public Microsoft::PSharp::Interop::Wrapper
	{
	private:
		ClientNativeComponents* _server_native_components;

	public:
		ClientWrapper(Microsoft::PSharp::MachineId^ mid);
		~ClientWrapper();

		virtual void invoke(Microsoft::PSharp::Event^ e) override;
	};
}
