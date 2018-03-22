using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TailSpin 
{
	[DataContract]
	class SurveyTag 
	{
		[DataMember]
		public int SubscriberId;
		[DataMember]
		public int response;

		public SurveyTag(int SubscriberId, int response)
		{
			this.SubscriberId = SubscriberId;
			this.response = response;
		}
	}
}
