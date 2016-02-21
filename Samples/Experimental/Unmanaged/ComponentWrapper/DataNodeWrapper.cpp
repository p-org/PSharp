#include <memory>

#include "DataNodeWrapper.h"
#include "DataNode.h"
#include "UpdateMessage.h"

public class DataNodeNativeComponents
{
public:
	DataNode* data_node;
};

Mocking::DataNodeWrapper::DataNodeWrapper(int idx)
{
	this->_native_components = new DataNodeNativeComponents();
	this->_native_components->data_node = new DataNode(idx);
	this->_idx = idx;
}

Mocking::DataNodeWrapper::~DataNodeWrapper()
{
	delete this->_native_components;
}

Events::MessageEvent^ Mocking::DataNodeWrapper::get_update()
{
	auto log = this->_native_components->data_node->create_log();
	auto msg = new UpdateMessage(this->_idx, log);
	return gcnew Events::MessageEvent(msg);
}
