#pragma once

#include "managed\psharp.h"

using namespace System;
using namespace System::Collections::Generic;

class NodeManagerNativeComponents;

namespace Mocking {

	public ref class NodeManagerWrapper
	{
	private:
		NodeManagerNativeComponents* _native_components;

	public:
		NodeManagerWrapper(List<Microsoft::PSharp::MachineId^>^ mids);
		~NodeManagerWrapper();

		void invoke(Microsoft::PSharp::Event^ e);
	};
}
