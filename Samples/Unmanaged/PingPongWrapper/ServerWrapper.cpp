#include <memory>

#include "ServerWrapper.h"
#include "Server.h"
#include "MockedNetworkEngine.h"

public class ServerNativeComponents
{
public:
	Mocking::MockedNetworkEngine* mocked_net_engine;
	Server* server;
};

PingPongWrapper::ServerWrapper::ServerWrapper(Microsoft::PSharp::Id^ mid)
	: Microsoft::PSharp::Interop::Wrapper()
{
	this->_server_native_components = new ServerNativeComponents();
	this->_server_native_components->mocked_net_engine = new Mocking::MockedNetworkEngine(mid);
	this->_server_native_components->server = new Server(
		this->_server_native_components->mocked_net_engine);
}

PingPongWrapper::ServerWrapper::~ServerWrapper()
{
	delete this->_server_native_components;
}

void PingPongWrapper::ServerWrapper::invoke(Microsoft::PSharp::Event^ e)
{
	this->_server_native_components->server->ping();
}

//void  PingPongWrapper::ServerWrapper::callback(Microsoft::PSharp::Event^ e)
//{
//
//}