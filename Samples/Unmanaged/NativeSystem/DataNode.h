#pragma once

#include <memory>

#include "Log.h"

class DataNode
{
private:
	int idx;
	Log* log;

public:
	DataNode(int idx);
	~DataNode();

	Log* create_log();
};