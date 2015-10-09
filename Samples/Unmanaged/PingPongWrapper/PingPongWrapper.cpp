// This is the main DLL file.

#include <memory>

#include "PingPongWrapper.h"
#include "Server.h"

void PingPongWrapper::ServerWrapper::invoke(Microsoft::PSharp::Event^ e)
{
	System::Console::WriteLine(gcnew System::String("Invoking ..."));
	auto server = std::make_shared<Server>();
	server->ping();
}