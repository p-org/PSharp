#include <memory>

#include "ClientWrapper.h"
#include "Client.h"
#include "MockedNetworkEngine.h"

public class ClientNativeComponents
{
public:
	Mocking::MockedNetworkEngine* mocked_net_engine;
	Client* client;
};

PingPongWrapper::ClientWrapper::ClientWrapper(Microsoft::PSharp::Id^ mid)
	: Microsoft::PSharp::Interop::Wrapper()
{
	this->_server_native_components = new ClientNativeComponents();
	this->_server_native_components->mocked_net_engine = new Mocking::MockedNetworkEngine(mid);
	this->_server_native_components->client = new Client(
		this->_server_native_components->mocked_net_engine);
}

PingPongWrapper::ClientWrapper::~ClientWrapper()
{
	delete this->_server_native_components;
}

void PingPongWrapper::ClientWrapper::invoke(Microsoft::PSharp::Event^ e)
{
	this->_server_native_components->client->pong();
}

//void PingPongWrapper::ClientWrapper::callback(Microsoft::PSharp::Event^ e)
//{
//
//}