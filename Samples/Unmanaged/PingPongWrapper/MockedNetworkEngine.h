#pragma once

#include <vcclr.h>
#include "managed\psharp.h"
#include "NetworkEngine.h"

using namespace System;

namespace Mocking {

	public class MockedNetworkEngine
		: public NetworkEngine
	{
	private:
		gcroot<Microsoft::PSharp::MachineId^> _target_machine_id;

	public:
		MockedNetworkEngine(Microsoft::PSharp::MachineId^ mid);
		~MockedNetworkEngine();
		virtual void send() override;
	};
}
