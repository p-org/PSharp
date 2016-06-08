#pragma once

#include <vcclr.h>
#include "managed\psharp.h"
#include "NetworkEngine.h"
#include "Log.h"

using namespace System;
using namespace System::Collections::Generic;

namespace Mocking {

	public class MockedNetworkEngine
		: public NetworkEngine
	{
	private:
		gcroot<List<Microsoft::PSharp::MachineId^>^> _target_machine_ids;

	public:
		MockedNetworkEngine(List<Microsoft::PSharp::MachineId^>^ mids);
		~MockedNetworkEngine();
		virtual void send(int idx, Log* log) override;
	};
}
