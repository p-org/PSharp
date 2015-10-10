#pragma once

#include "managed\psharp.h"
#include "NetworkEngine.h"

using namespace System;

namespace Mocking {

	public class MockedNetworkEngine
		: public NetworkEngine
	{
	public:
		MockedNetworkEngine();
		~MockedNetworkEngine();
		virtual void send() override;
	};
}
