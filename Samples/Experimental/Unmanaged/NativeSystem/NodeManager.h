#pragma once

#include <memory>
#include <map>

#include "NetworkEngine.h"
#include "Log.h"

class NodeManager
{
private:
	NetworkEngine* _net_engine;
	std::map<int, Log*> _data_log;

public:
	NodeManager(NetworkEngine* engine);
	~NodeManager();

	void store_log(int idx, Log* log);
	void update_node(int idx, Log* log);
};