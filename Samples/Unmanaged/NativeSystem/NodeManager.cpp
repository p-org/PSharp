#include <iostream>

#include "NodeManager.h"

NodeManager::NodeManager(NetworkEngine* engine)
{
	this->_net_engine = engine;
}

NodeManager::~NodeManager()
{
	delete this->_net_engine;
}

void NodeManager::store_log(int idx, Log* log)
{
	std::cout << "NodeManager received data from node " << idx << ".\n" << std::endl;
	this->_data_log[idx] = log;
}

void NodeManager::update_node(int idx, Log* log)
{
	std::cout << "NodeManager updates node " << idx << ".\n" << std::endl;
	this->_net_engine->send(idx, log);
}
