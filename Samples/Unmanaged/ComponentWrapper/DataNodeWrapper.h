#pragma once

#include "managed\psharp.h"
#include "Events.h"

using namespace System;
using namespace System::Collections::Generic;

class DataNodeNativeComponents;

namespace Mocking {

	public ref class DataNodeWrapper
	{
	private:
		DataNodeNativeComponents* _native_components;
		int _idx;

	public:
		DataNodeWrapper(int idx);
		~DataNodeWrapper();

		Events::MessageEvent^ get_update();
	};
}
