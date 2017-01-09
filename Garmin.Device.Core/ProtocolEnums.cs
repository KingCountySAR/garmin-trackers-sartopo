using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garmin.Device.Core
{
  public enum L000_packet_id
  {
    Pid_Protocol_Array = 253,  /* may not be implemented in all devices */
    Pid_Product_Rqst = 254,
    Pid_Product_Data = 255,
    Pid_Ext_Product_Data = 248  /* may not be implemented in all devices */
  }
}
